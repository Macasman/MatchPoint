using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces;

public interface IPaymentIntentRepository
{
    Task<long> CreateAsync(PaymentIntent entity, CancellationToken ct);
    Task<PaymentIntent?> GetByIdAsync(long id, CancellationToken ct);
    Task<bool> CaptureAsync(long id, CancellationToken ct);
}
