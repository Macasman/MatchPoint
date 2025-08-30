// MatchPoint.Application/Reservations/Commands/CancelReservationCommand.cs
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Interfaces;
using static MatchPoint.Domain.Enums.Enums; // ReservationStatus

public sealed record CancelReservationCommand(long ReservationId) : IRequest<bool>;

public sealed class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand, bool>
{
    private readonly IReservationRepository _repo;
    private readonly IPaymentIntentRepository _payments;
    public CancelReservationCommandHandler(IReservationRepository repo) => _repo = repo;

    public CancelReservationCommandHandler(IReservationRepository repo, IPaymentIntentRepository payments)
    {
        _repo = repo;
        _payments = payments; // 👈
    }

    public async Task<bool> Handle(CancelReservationCommand cmd, CancellationToken ct)
    {
        var ok = await _repo.CancelAsync(cmd.ReservationId, ct);
        if (!ok) return false;

        // 👇 tenta cancelar intents pendentes/autorizadas vinculadas
        await _payments.CancelByReservationAsync(cmd.ReservationId, ct);
        return true;
    }
}
