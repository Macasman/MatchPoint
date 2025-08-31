using System;
using System.Data;
using System.Data.SqlClient;                 // mantém seu provider atual
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MatchPoint.Application.Interfaces;
using MatchPoint.Infrastructure.Persistence;

namespace MatchPoint.Infrastructure.Messaging
{
    public sealed class WebhookQueueSqlWriter : IWebhookQueueWriter
    {
        private readonly SqlDbContext _db;
        public WebhookQueueSqlWriter(SqlDbContext db) => _db = db;

        public async Task<long> EnqueuePaymentEventAsync(
            long paymentIntentId,
            string @event,
            string? providerRef = null,
            DateTime? scheduleUtc = null,
            CancellationToken ct = default)
        {
            // ❗ Use shorthand com @event para gerar a propriedade JSON "event"
            var payload = JsonSerializer.Serialize(new { paymentIntentId, @event, providerRef });

            using var conn = _db.CreateConnection();
            await conn.OpenAsync(ct);

            using var cmd = new SqlCommand(@"
                INSERT INTO integration.WebhookQueue
                    (AggregateType, AggregateId, Payload, Status, NextAttemptUtc)
                VALUES
                    ('PaymentIntent', @pid, @payload, 0, @next);
                SELECT CONVERT(BIGINT, SCOPE_IDENTITY());
            ", conn);

            cmd.Parameters.Add(new SqlParameter("@pid", SqlDbType.BigInt) { Value = paymentIntentId });
            // NVARCHAR(MAX) => tamanho -1
            cmd.Parameters.Add(new SqlParameter("@payload", SqlDbType.NVarChar, -1) { Value = payload });
            cmd.Parameters.Add(new SqlParameter("@next", SqlDbType.DateTime2)
            { Value = (object?)(scheduleUtc ?? DateTime.UtcNow) ?? DBNull.Value });

            var id = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }
    }
}
