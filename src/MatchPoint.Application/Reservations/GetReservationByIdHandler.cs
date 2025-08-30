using MediatR;
using MatchPoint.Domain.Entities;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.Application.Reservations;

public class GetReservationByIdHandler : IRequestHandler<GetReservationByIdQuery, Reservation?>
{
    private readonly IReservationRepository _repo;
    public GetReservationByIdHandler(IReservationRepository repo) => _repo = repo;

    public Task<Reservation?> Handle(GetReservationByIdQuery req, CancellationToken ct)
        => _repo.GetByIdAsync(req.ReservationId, ct);
}
