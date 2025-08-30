using System.Data.SqlClient;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;
using static MatchPoint.Domain.Enums.Enums;

namespace MatchPoint.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly SqlDbContext _db;
    public ReservationRepository(SqlDbContext db) => _db = db;

    public async Task<long> CreateAsync(Reservation e, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO booking.Reservations
(UserId, ResourceId, StartTime, EndTime, Status, PriceCents, Currency, Notes)
OUTPUT INSERTED.ReservationId
VALUES (@UserId, @ResourceId, @StartTime, @EndTime, @Status, @PriceCents, @Currency, @Notes);";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", e.UserId);
        cmd.Parameters.AddWithValue("@ResourceId", e.ResourceId);
        cmd.Parameters.AddWithValue("@StartTime", e.StartTime);
        cmd.Parameters.AddWithValue("@EndTime", e.EndTime);
        cmd.Parameters.AddWithValue("@Status", e.Status);
        cmd.Parameters.AddWithValue("@PriceCents", e.PriceCents);
        cmd.Parameters.AddWithValue("@Currency", e.Currency);
        cmd.Parameters.AddWithValue("@Notes", (object?)e.Notes ?? DBNull.Value);

        return (long)await cmd.ExecuteScalarAsync(ct);
    }

    public async Task<Reservation?> GetByIdAsync(long id, CancellationToken ct)
    {
        const string sql = @"
SELECT ReservationId, UserId, ResourceId, StartTime, EndTime, Status, PriceCents, Currency, Notes, CreationDate, UpdateDate
FROM booking.Reservations WITH (NOLOCK)
WHERE ReservationId = @Id;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);


        using var rd = await cmd.ExecuteReaderAsync(ct);
        int iStatus = rd.GetOrdinal("Status");
        if (!await rd.ReadAsync(ct)) return null;

        return new Reservation
        {
            ReservationId = rd.GetInt64(0),
            UserId = rd.GetInt64(1),
            ResourceId = rd.GetInt64(2),
            StartTime = rd.GetDateTime(3),
            EndTime = rd.GetDateTime(4),
            Status = (ReservationStatus)rd.GetByte(iStatus),
            PriceCents = rd.GetInt32(6),
            Currency = rd.GetString(7),
            Notes = rd.IsDBNull(8) ? null : rd.GetString(8),
            CreationDate = rd.GetDateTime(9),
            UpdateDate = rd.IsDBNull(10) ? null : rd.GetDateTime(10)
        };
    }
}
