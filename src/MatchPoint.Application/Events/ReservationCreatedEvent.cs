namespace MatchPoint.Application.Events;

public class ReservationCreatedEvent
{
    public long ReservationId { get; set; }
    public long UserId { get; set; }
    public long ResourceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string EventName => "ReservationCreated";
    public string SchemaVersion => "1.0";
}
