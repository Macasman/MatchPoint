// MatchPoint.Application/Common/AuditHelper.cs
using System.Text.Json;
using MatchPoint.Domain;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Common
{
    public static class AuditHelper
    {
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public static AuditEvent Build(string aggregate, long aggregateId, string action, object? data = null, long? userId = null)
            => new AuditEvent
            {
                Aggregate = aggregate,
                AggregateId = aggregateId,
                Action = action,
                Data = data is null ? null : JsonSerializer.Serialize(data, _json),
                UserId = userId
            };
    }
}
