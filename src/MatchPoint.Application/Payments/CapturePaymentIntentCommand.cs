using MediatR;

namespace MatchPoint.Application.Payments;
public record CapturePaymentIntentCommand(long PaymentIntentId) : IRequest<bool>;
