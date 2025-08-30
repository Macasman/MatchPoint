using MediatR;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Reservations;

public record GetReservationByIdQuery(long ReservationId) : IRequest<Reservation?>;
