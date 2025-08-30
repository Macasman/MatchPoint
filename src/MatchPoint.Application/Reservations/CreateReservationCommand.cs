using MediatR;

namespace MatchPoint.Application.Reservations;

public record CreateReservationCommand(
    long UserId,
    long ResourceId,
    DateTime StartTime,
    DateTime EndTime,
    int PriceCents,
    string Currency,
    string? Notes
) : IRequest<long>;
