namespace MatchPoint.Domain.Entities;

public class PaymentIntent
{
    public long PaymentIntentId { get; set; }
    public long ReservationId { get; set; }
    public int AmountCents { get; set; }
    public string Currency { get; set; } = "BRL";
    public byte Status { get; set; } = 1; // 1=Pending, 2=Authorized, 3=Captured, 4=Failed, 5=Canceled
    public string? Provider { get; set; }
    public string? ProviderRef { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime? UpdateDate { get; set; }
}
