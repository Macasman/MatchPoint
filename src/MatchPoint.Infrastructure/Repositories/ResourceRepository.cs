using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Abstractions;
using MatchPoint.Infrastructure.Persistence;

namespace MatchPoint.Infrastructure.Repositories
{
    public sealed class ResourceRepository : IResourceRepository
    {
        private readonly SqlDbContext _db;
        public ResourceRepository(SqlDbContext db) => _db = db;

        public async Task<long> CreateAsync(Resource e, CancellationToken ct)
        {
           
            const string sql = @"
                INSERT INTO booking.Resources (Name, Location, PricePerHourCents, Currency, IsActive)
                OUTPUT INSERTED.ResourceId
                VALUES (@Name, @Location, @PricePerHourCents, @Currency, @IsActive);";

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);

            try
            {
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 120) { Value = e.Name });
                cmd.Parameters.Add(new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)e.Location ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@PricePerHourCents", SqlDbType.Int) { Value = e.PricePerHourCents });
                cmd.Parameters.Add(new SqlParameter("@Currency", SqlDbType.Char, 3) { Value = e.Currency });
                cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = e.IsActive });

                var id = await cmd.ExecuteScalarAsync(ct);
                return Convert.ToInt64(id);
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                // Mensagem enriquecida para localizar o problema no ambiente certo
                throw new InvalidOperationException(
                    "SQL 208 (Invalid object name 'booking.Resources'): a tabela/schema não existem na base em que a API está conectada. " +
                    "Rode o script de criação na MESMA base/servidor que a conexão está usando (veja o diagnóstico acima).", ex);
            }
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

            int i(string col) => rd.GetOrdinal(col);
            return new Resource
            {
                ResourceId = rd.GetInt64(i("ResourceId")),
                Name = rd.GetString(i("Name")),
                Location = rd.IsDBNull(i("Location")) ? null : rd.GetString(i("Location")),
                PricePerHourCents = rd.GetInt32(i("PricePerHourCents")),
                Currency = rd.GetString(i("Currency")),
                IsActive = rd.GetBoolean(i("IsActive")),
                CreationDate = rd.GetDateTime(i("CreationDate")),
                UpdateDate = rd.IsDBNull(i("UpdateDate")) ? (DateTime?)null : rd.GetDateTime(i("UpdateDate"))
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
            int iId = rd.GetOrdinal("ResourceId");
            int iName = rd.GetOrdinal("Name");
            int iLoc = rd.GetOrdinal("Location");
            int iPrice = rd.GetOrdinal("PricePerHourCents");
            int iCur = rd.GetOrdinal("Currency");
            int iAct = rd.GetOrdinal("IsActive");
            int iCre = rd.GetOrdinal("CreationDate");
            int iUpd = rd.GetOrdinal("UpdateDate");

            while (await rd.ReadAsync(ct))
            {
                list.Add(new Resource
                {
                    ResourceId = rd.GetInt64(iId),
                    Name = rd.GetString(iName),
                    Location = rd.IsDBNull(iLoc) ? null : rd.GetString(iLoc),
                    PricePerHourCents = rd.GetInt32(iPrice),
                    Currency = rd.GetString(iCur),
                    IsActive = rd.GetBoolean(iAct),
                    CreationDate = rd.GetDateTime(iCre),
                    UpdateDate = rd.IsDBNull(iUpd) ? (DateTime?)null : rd.GetDateTime(iUpd)
                });
            }
            return list;
        }

        public async Task<bool> UpdateAsync(Resource e, CancellationToken ct)
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
            cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar, 120) { Value = e.Name });
            cmd.Parameters.Add(new SqlParameter("@Location", SqlDbType.NVarChar, 200) { Value = (object?)e.Location ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PricePerHourCents", SqlDbType.Int) { Value = e.PricePerHourCents });
            cmd.Parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = e.IsActive });
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.BigInt) { Value = e.ResourceId });

            var rows = await cmd.ExecuteNonQueryAsync(ct);
            return rows > 0;
        }
    }
}
