using System.Data;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;
using static MatchPoint.Domain.Enums.Enums;

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

        int iStatus = rd.GetOrdinal("Status");

        return new PaymentIntent
        {
            PaymentIntentId = rd.GetInt64(0),
            ReservationId = rd.GetInt64(1),
            AmountCents = rd.GetInt32(2),
            Currency = rd.GetString(3),
            Status = (PaymentIntentStatus)rd.GetByte(iStatus),
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

    public async Task<long?> CreateForReservationAsync(long reservationId, int amountCents, string currency, string? provider, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO payments.PaymentIntents (ReservationId, AmountCents, Currency, Status, Provider, CreationDate)
            OUTPUT INSERTED.PaymentIntentId
            VALUES (@ReservationId, @AmountCents, @Currency, @Status, @Provider, SYSUTCDATETIME());";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@ReservationId", SqlDbType.BigInt) { Value = reservationId });
        cmd.Parameters.Add(new SqlParameter("@AmountCents", SqlDbType.Int) { Value = amountCents });
        cmd.Parameters.Add(new SqlParameter("@Currency", SqlDbType.Char, 3) { Value = currency });
        cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.TinyInt) { Value = (byte)PaymentIntentStatus.Pending });
        cmd.Parameters.Add(new SqlParameter("@Provider", SqlDbType.NVarChar, 100) { Value = (object?)provider ?? DBNull.Value });

        var id = await cmd.ExecuteScalarAsync(ct);
        return id is null ? (long?)null : Convert.ToInt64(id);
    }

    public async Task<bool> CancelByReservationAsync(long reservationId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE payments.PaymentIntents
               SET Status = @Canceled, UpdateDate = SYSUTCDATETIME()
             WHERE ReservationId = @ReservationId
               AND Status IN (@Pending, @Authorized);";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@ReservationId", SqlDbType.BigInt) { Value = reservationId });
        cmd.Parameters.Add(new SqlParameter("@Canceled", SqlDbType.TinyInt) { Value = (byte)PaymentIntentStatus.Canceled });
        cmd.Parameters.Add(new SqlParameter("@Pending", SqlDbType.TinyInt) { Value = (byte)PaymentIntentStatus.Pending });
        cmd.Parameters.Add(new SqlParameter("@Authorized", SqlDbType.TinyInt) { Value = (byte)PaymentIntentStatus.Authorized });

        var rows = await cmd.ExecuteNonQueryAsync(ct);
        return rows > 0;
    }

}
