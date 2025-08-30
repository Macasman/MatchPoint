using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Abstractions;

namespace MatchPoint.Infrastructure.Repositories
{
    public sealed class ResourceRepository : IResourceRepository
    {
        private readonly ISqlConnectionFactory _db;
        public ResourceRepository(ISqlConnectionFactory db) => _db = db;

        public async Task<long> CreateAsync(Resource r, CancellationToken ct)
        {
            const string sql = @"
INSERT INTO booking.Resources (Name, Location, PricePerHourCents, Currency, IsActive)
OUTPUT INSERTED.ResourceId
VALUES (@Name, @Location, @PricePerHourCents, @Currency, @IsActive);";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 120) { Value = r.Name });
            cmd.Parameters.Add(new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)r.Location ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PricePerHourCents", SqlDbType.Int) { Value = r.PricePerHourCents });
            cmd.Parameters.Add(new SqlParameter("@Currency", SqlDbType.Char, 3) { Value = r.Currency });
            cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = r.IsActive });

            var id = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }

        public async Task<Resource?> GetByIdAsync(long id, CancellationToken ct)
        {
            const string sql = @"
SELECT ResourceId, Name, Location, PricePerHourCents, Currency, IsActive, CreationDate, UpdateDate
FROM booking.Resources WITH (NOLOCK)
WHERE ResourceId = @Id;";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = id });

            using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, ct);
            if (!await rd.ReadAsync(ct)) return null;

            int iId = rd.GetOrdinal("ResourceId");
            int iName = rd.GetOrdinal("Name");
            int iLocation = rd.GetOrdinal("Location");
            int iPrice = rd.GetOrdinal("PricePerHourCents");
            int iCurrency = rd.GetOrdinal("Currency");
            int iActive = rd.GetOrdinal("IsActive");
            int iCreation = rd.GetOrdinal("CreationDate");
            int iUpdate = rd.GetOrdinal("UpdateDate");

            return new Resource
            {
                ResourceId = rd.GetInt64(iId),
                Name = rd.GetString(iName),
                Location = rd.IsDBNull(iLocation) ? null : rd.GetString(iLocation),
                PricePerHourCents = rd.GetInt32(iPrice),
                Currency = rd.GetString(iCurrency),
                IsActive = rd.GetBoolean(iActive),
                CreationDate = rd.GetDateTime(iCreation),
                UpdateDate = rd.IsDBNull(iUpdate) ? (DateTime?)null : rd.GetDateTime(iUpdate)
            };
        }

        public async Task<IReadOnlyList<Resource>> ListAsync(bool? onlyActive, CancellationToken ct)
        {
            string sql = @"
SELECT ResourceId, Name, Location, PricePerHourCents, Currency, IsActive, CreationDate, UpdateDate
FROM booking.Resources WITH (NOLOCK)";
            if (onlyActive.HasValue)
                sql += " WHERE IsActive = @Active";
            sql += " ORDER BY Name ASC;";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand(sql, conn);
            if (onlyActive.HasValue)
                cmd.Parameters.Add(new SqlParameter("@Active", SqlDbType.Bit) { Value = onlyActive.Value });

            using var rd = await cmd.ExecuteReaderAsync(ct);
            var list = new List<Resource>();
            while (await rd.ReadAsync(ct))
            {
                int iId = rd.GetOrdinal("ResourceId");
                int iName = rd.GetOrdinal("Name");
                int iLocation = rd.GetOrdinal("Location");
                int iPrice = rd.GetOrdinal("PricePerHourCents");
                int iCurrency = rd.GetOrdinal("Currency");
                int iActive = rd.GetOrdinal("IsActive");
                int iCreation = rd.GetOrdinal("CreationDate");
                int iUpdate = rd.GetOrdinal("UpdateDate");

                list.Add(new Resource
                {
                    ResourceId = rd.GetInt64(iId),
                    Name = rd.GetString(iName),
                    Location = rd.IsDBNull(iLocation) ? null : rd.GetString(iLocation),
                    PricePerHourCents = rd.GetInt32(iPrice),
                    Currency = rd.GetString(iCurrency),
                    IsActive = rd.GetBoolean(iActive),
                    CreationDate = rd.GetDateTime(iCreation),
                    UpdateDate = rd.IsDBNull(iUpdate) ? (DateTime?)null : rd.GetDateTime(iUpdate)
                });
            }
            return list;
        }

        public async Task<bool> UpdateAsync(Resource r, CancellationToken ct)
        {
            const string sql = @"
UPDATE booking.Resources
   SET Name = @Name,
       Location = @Location,
       PricePerHourCents = @PricePerHourCents,
       IsActive = @IsActive,
       UpdateDate = SYSUTCDATETIME()
 WHERE ResourceId = @Id;";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 120) { Value = r.Name });
            cmd.Parameters.Add(new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)r.Location ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PricePerHourCents", SqlDbType.Int) { Value = r.PricePerHourCents });
            cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = r.IsActive });
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = r.ResourceId });

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }
    }
}
