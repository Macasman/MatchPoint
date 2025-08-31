namespace MatchPoint.Infrastructure.Logging;

public sealed class MongoAuditLogOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "matchpoint";
    public string Collection { get; set; } = "api_audit_logs";
    // Se > 0, cria índice TTL baseado em TimestampUtc
    public int TtlDays { get; set; } = 30;
}
