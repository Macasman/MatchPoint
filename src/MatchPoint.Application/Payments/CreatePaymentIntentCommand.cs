using MediatR;

namespace MatchPoint.Application.Payments;

public record CreatePaymentIntentCommand(
    long ReservationId,
    int AmountCents,
    string Currency = "BRL",
    string? Provider = "Simulado"
) : IRequest<long>;
