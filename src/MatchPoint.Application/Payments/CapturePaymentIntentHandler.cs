using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Payments;

public class CapturePaymentIntentHandler : IRequestHandler<CapturePaymentIntentCommand, bool>
{
    private readonly IPaymentIntentRepository _repo;
    private readonly IAuditRepository _audit;
    public CapturePaymentIntentHandler(IPaymentIntentRepository repo) => _repo = repo;

    public async Task<bool> Handle(CapturePaymentIntentCommand req, CancellationToken ct)
    {
        var ok = await _repo.CaptureAsync(req.PaymentIntentId, ct);

        if (ok)
        {
            await _audit.LogAsync(new AuditEvent
            {
                Aggregate = "PaymentIntent",
                AggregateId = req.PaymentIntentId,
                Action = "Captured",
                Data = $"{{ \"Captured\": true }}",
                UserId = null // ou vir do token JWT
            }, ct);
        }

        return ok;
    }

}
