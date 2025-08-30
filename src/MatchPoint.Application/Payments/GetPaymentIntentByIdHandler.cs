using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Payments;

public class GetPaymentIntentByIdHandler : IRequestHandler<GetPaymentIntentByIdQuery, PaymentIntent?>
{
    private readonly IPaymentIntentRepository _repo;
    public GetPaymentIntentByIdHandler(IPaymentIntentRepository repo) => _repo = repo;

    public Task<PaymentIntent?> Handle(GetPaymentIntentByIdQuery req, CancellationToken ct)
        => _repo.GetByIdAsync(req.PaymentIntentId, ct);
}
