using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Payments;

public class CreatePaymentIntentHandler : IRequestHandler<CreatePaymentIntentCommand, long>
{
    private readonly IPaymentIntentRepository _repo;
    public CreatePaymentIntentHandler(IPaymentIntentRepository repo) => _repo = repo;

    public async Task<long> Handle(CreatePaymentIntentCommand req, CancellationToken ct)
    {
        if (req.AmountCents <= 0) throw new ArgumentException("AmountCents must be > 0");
        var pi = new PaymentIntent
        {
            ReservationId = req.ReservationId,
            AmountCents = req.AmountCents,
            Currency = string.IsNullOrWhiteSpace(req.Currency) ? "BRL" : req.Currency,
            Status = 1,
            Provider = req.Provider
        };
        return await _repo.CreateAsync(pi, ct);
    }
}
