using System.Data.SqlClient;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;

namespace MatchPoint.Infrastructure.Repositories;

public class PaymentIntentRepository : IPaymentIntentRepository
{
    private readonly SqlDbContext _db;
    public PaymentIntentRepository(SqlDbContext db) => _db = db;

    public async Task<long> CreateAsync(PaymentIntent e, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO payments.PaymentIntents
(ReservationId, AmountCents, Currency, Status, Provider, ProviderRef)
OUTPUT INSERTED.PaymentIntentId
VALUES (@ReservationId, @AmountCents, @Currency, @Status, @Provider, @ProviderRef);";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ReservationId", e.ReservationId);
        cmd.Parameters.AddWithValue("@AmountCents", e.AmountCents);
        cmd.Parameters.AddWithValue("@Currency", e.Currency);
        cmd.Parameters.AddWithValue("@Status", e.Status);           // 1=Pending
        cmd.Parameters.AddWithValue("@Provider", (object?)e.Provider ?? "Simulado");
        cmd.Parameters.AddWithValue("@ProviderRef", (object?)e.ProviderRef ?? DBNull.Value);
        return (long)await cmd.ExecuteScalarAsync(ct);
    }

    public async Task<PaymentIntent?> GetByIdAsync(long id, CancellationToken ct)
    {
        const string sql = @"
SELECT PaymentIntentId, ReservationId, AmountCents, Currency, Status, Provider, ProviderRef, CreationDate, UpdateDate
FROM payments.PaymentIntents WITH (NOLOCK)
WHERE PaymentIntentId = @Id;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;

        return new PaymentIntent
        {
            PaymentIntentId = rd.GetInt64(0),
            ReservationId = rd.GetInt64(1),
            AmountCents = rd.GetInt32(2),
            Currency = rd.GetString(3),
            Status = rd.GetByte(4),
            Provider = rd.IsDBNull(5) ? null : rd.GetString(5),
            ProviderRef = rd.IsDBNull(6) ? null : rd.GetString(6),
            CreationDate = rd.GetDateTime(7),
            UpdateDate = rd.IsDBNull(8) ? null : rd.GetDateTime(8),
        };
    }

    public async Task<bool> CaptureAsync(long id, CancellationToken ct)
    {
        const string sql = @"
UPDATE payments.PaymentIntents
SET Status = 3, UpdateDate = SYSUTCDATETIME()  -- 3=Captured
WHERE PaymentIntentId = @Id AND Status IN (1,2); -- Pending/Authorized";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }
}
