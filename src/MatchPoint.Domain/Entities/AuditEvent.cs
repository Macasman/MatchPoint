namespace MatchPoint.Domain.Entities;

public class AuditEvent
{
    public long AuditId { get; set; }
    public string Aggregate { get; set; } = string.Empty;
    public long AggregateId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Data { get; set; }
    public long? UserId { get; set; }
    public DateTime CreationDate { get; set; }
}
