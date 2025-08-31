using static MatchPoint.Domain.Enums.Enums;

namespace MatchPoint.Domain.Entities;

public class Reservation
{
    public long ReservationId { get; set; }
    public long UserId { get; set; }
    public long ResourceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; }     // 1=Agendada
    public int PriceCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public string? Notes { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}
