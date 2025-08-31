// MatchPoint.Application/Reservations/Commands/CancelReservationCommand.cs
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Interfaces;
using static MatchPoint.Domain.Enums.Enums;
using MatchPoint.Application.Common; // ReservationStatus

public sealed record CancelReservationCommand(long ReservationId) : IRequest<bool>;

public sealed class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, bool>
{
    private readonly IReservationRepository _repo;
    private readonly IPaymentIntentRepository _payments;
    private readonly IAuditRepository _audit;
    public CancelReservationCommandHandler(IReservationRepository repo) => _repo = repo;

    public CancelReservationCommandHandler(IReservationRepository repo, IPaymentIntentRepository payments, IAuditRepository audit)
    {
        _repo = repo;
        _payments = payments; // 👈
        _audit = audit;
    }

    public async Task<bool> Handle(CancelReservationCommand cmd, CancellationToken ct)
    {
        var ok = await _repo.CancelAsync(cmd.ReservationId, ct);
        if (!ok) return false;

        await _audit.LogAsync(
            AuditHelper.Build("Reservation", cmd.ReservationId, "Canceled"),
            ct);

        var piCanceled = await _payments.CancelByReservationAsync(cmd.ReservationId, ct);
        if (piCanceled)
        {
            await _audit.LogAsync(
                AuditHelper.Build("PaymentIntent", 0, "CanceledByReservation", new { ReservationId = cmd.ReservationId }),
                ct);
        }
        return true;
    }
}
