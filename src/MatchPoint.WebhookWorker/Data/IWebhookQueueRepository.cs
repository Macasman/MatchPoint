using System.Data;
using Microsoft.Data.SqlClient;
using MatchPoint.WebhookWorker.Models;
using static MatchPoint.WebhookWorker.Enums.Enums;

namespace MatchPoint.WebhookWorker.Data;

public interface IWebhookQueueRepository
{
    Task<IReadOnlyList<WebhookJob>> DequeueBatchAsync(int max, CancellationToken ct);
    Task AckSuccessAsync(long id, CancellationToken ct);
    Task AckFailureAsync(long id, int attempts, int maxAttempts, int backoffSecondsBase, string? error, CancellationToken ct);
}

public sealed class WebhookQueueRepository : IWebhookQueueRepository
{
    private readonly SqlDbContext _db;
    public WebhookQueueRepository(SqlDbContext db) => _db = db;

    public async Task<IReadOnlyList<WebhookJob>> DequeueBatchAsync(int max, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

        // Seleciona TOP(@max) Pending prontos (READPAST evita bloqueios), marca como Processing e retorna os registros
        var table = new DataTable();
        using (var cmd = new SqlCommand(@"
            DECLARE @picked TABLE (Id BIGINT);

            UPDATE integration.WebhookQueue WITH (ROWLOCK, READPAST, UPDLOCK)
            SET Status = @processing, UpdateDate = SYSUTCDATETIME()
            OUTPUT inserted.Id INTO @picked(Id)
            WHERE Id IN (
                SELECT TOP (@max) Id
                FROM integration.WebhookQueue WITH (ROWLOCK, READPAST, UPDLOCK)
                WHERE Status = @pending AND NextAttemptUtc <= SYSUTCDATETIME()
                ORDER BY CreationDate ASC
            );

            SELECT q.Id, q.AggregateType, q.AggregateId, q.Payload, q.Attempts
            FROM integration.WebhookQueue q
            JOIN @picked p ON p.Id = q.Id
            ORDER BY q.Id ASC;
        ", conn, tx))
        {
            cmd.Parameters.AddWithValue("@max", max);
            cmd.Parameters.AddWithValue("@pending", (byte)WebhookStatus.Pending);
            cmd.Parameters.AddWithValue("@processing", (byte)WebhookStatus.Processing);

            using var rd = await cmd.ExecuteReaderAsync(ct);
            table.Load(rd);
        }

        await tx.CommitAsync(ct);

        var list = new List<WebhookJob>(table.Rows.Count);
        foreach (DataRow r in table.Rows)
        {
            list.Add(new WebhookJob
            {
                Id = (long)r["Id"],
                AggregateType = (string)r["AggregateType"],
                AggregateId = (long)r["AggregateId"],
                Payload = (string)r["Payload"],
                Attempts = (int)r["Attempts"]
            });
        }
        return list;
    }

    public async Task AckSuccessAsync(long id, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);
        using var cmd = new SqlCommand(@"
            UPDATE integration.WebhookQueue
            SET Status = @sent, Attempts = Attempts + 1, LastError = NULL, UpdateDate = SYSUTCDATETIME()
            WHERE Id = @id;
        ", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@sent", (byte)WebhookStatus.Sent);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task AckFailureAsync(long id, int attempts, int maxAttempts, int backoffSecondsBase, string? error, CancellationToken ct)
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync(ct);

        // Calcula backoff exponencial (minutos/segundos conforme base)
        // Próxima tentativa = now + base * 2^(attempts) (limitado)
        var nextSeconds = (int)Math.Min(backoffSecondsBase * Math.Pow(2, Math.Min(attempts, 10)), 3600); // cap 1h

        var status = (attempts + 1 >= maxAttempts) ? WebhookStatus.DeadLetter : WebhookStatus.Failed;

        using var cmd = new SqlCommand(@"
            UPDATE integration.WebhookQueue
            SET Status = @status,
                Attempts = Attempts + 1,
                LastError = LEFT(@err, 1000),
                NextAttemptUtc = DATEADD(second, @nextSec, SYSUTCDATETIME()),
                UpdateDate = SYSUTCDATETIME()
            WHERE Id = @id;
        ", conn);

        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@status", (byte)status);
        cmd.Parameters.AddWithValue("@err", (object?)error ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@nextSec", nextSeconds);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
