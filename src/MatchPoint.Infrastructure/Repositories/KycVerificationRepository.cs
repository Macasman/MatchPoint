using System.Data.SqlClient;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;

namespace MatchPoint.Infrastructure.Repositories;

public class KycVerificationRepository : IKycVerificationRepository
{
    private readonly SqlDbContext _db;
    public KycVerificationRepository(SqlDbContext db) => _db = db;

    public async Task<long> CreateAsync(KycVerification e, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO kyc.KycVerifications (UserId, Status, Provider, Score, Notes)
OUTPUT INSERTED.KycId
VALUES (@UserId, @Status, @Provider, @Score, @Notes);";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", e.UserId);
        cmd.Parameters.AddWithValue("@Status", e.Status);
        cmd.Parameters.AddWithValue("@Provider", (object?)e.Provider ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Score", (object?)e.Score ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", (object?)e.Notes ?? DBNull.Value);

        return (long)await cmd.ExecuteScalarAsync(ct);
    }

    public async Task<KycVerification?> GetByIdAsync(long kycId, CancellationToken ct)
    {
        const string sql = @"
SELECT KycId, UserId, Status, Provider, Score, Notes, CreationDate, UpdateDate
FROM kyc.KycVerifications WITH (NOLOCK)
WHERE KycId = @KycId;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@KycId", kycId);

        using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;

        return new KycVerification
        {
            KycId = rd.GetInt64(0),
            UserId = rd.GetInt64(1),
            Status = rd.GetByte(2),
            Provider = rd.IsDBNull(3) ? null : rd.GetString(3),
            Score = rd.IsDBNull(4) ? null : rd.GetDecimal(4),
            Notes = rd.IsDBNull(5) ? null : rd.GetString(5),
            CreationDate = rd.GetDateTime(6),
            UpdateDate = rd.IsDBNull(7) ? null : rd.GetDateTime(7)
        };
    }

    public async Task<KycVerification?> GetLatestByUserAsync(long userId, CancellationToken ct)
    {
        const string sql = @"
SELECT TOP(1) KycId, UserId, Status, Provider, Score, Notes, CreationDate, UpdateDate
FROM kyc.KycVerifications WITH (NOLOCK)
WHERE UserId = @UserId
ORDER BY CreationDate DESC;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@UserId", userId);

        using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;

        return new KycVerification
        {
            KycId = rd.GetInt64(0),
            UserId = rd.GetInt64(1),
            Status = rd.GetByte(2),
            Provider = rd.IsDBNull(3) ? null : rd.GetString(3),
            Score = rd.IsDBNull(4) ? null : rd.GetDecimal(4),
            Notes = rd.IsDBNull(5) ? null : rd.GetString(5),
            CreationDate = rd.GetDateTime(6),
            UpdateDate = rd.IsDBNull(7) ? null : rd.GetDateTime(7)
        };
    }

    public async Task<bool> UpdateStatusAsync(long kycId, byte status, decimal? score, string? notes, CancellationToken ct)
    {
        const string sql = @"
UPDATE kyc.KycVerifications
SET Status = @Status,
    Score = @Score,
    Notes = @Notes,
    UpdateDate = SYSUTCDATETIME()
WHERE KycId = @KycId;";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@KycId", kycId);
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Score", (object?)score ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }
}
