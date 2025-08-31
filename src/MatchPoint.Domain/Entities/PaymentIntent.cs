using static MatchPoint.Domain.Enums.Enums;

namespace MatchPoint.Domain.Entities;

public class PaymentIntent
{
    public long PaymentIntentId { get; set; }
    public long ReservationId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public PaymentIntentStatus Status { get; set; } 
    public string? Provider { get; set; }
    public string? ProviderRef { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}
