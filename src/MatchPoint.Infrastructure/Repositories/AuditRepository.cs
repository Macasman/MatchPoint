using System.Data.SqlClient;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using MatchPoint.Infrastructure.Persistence;

namespace MatchPoint.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly SqlDbContext _db;
    public AuditRepository(SqlDbContext db) => _db = db;

    public async Task LogAsync(AuditEvent e, CancellationToken ct)
    {
        // ATENÇÃO: usa os nomes de colunas do seu banco atual (DataJson, ActorUserId)
        const string sql = @"
INSERT INTO audit.AuditEvents (Aggregate, AggregateId, Action, DataJson, ActorUserId)
VALUES (@Aggregate, @AggregateId, @Action, @DataJson, @ActorUserId);";

        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@Aggregate", e.Aggregate);
        cmd.Parameters.AddWithValue("@AggregateId", e.AggregateId);
        cmd.Parameters.AddWithValue("@Action", e.Action);
        cmd.Parameters.AddWithValue("@DataJson", (object?)e.Data ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ActorUserId", (object?)e.UserId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
