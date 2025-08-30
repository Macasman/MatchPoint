using MediatR;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Payments;

public record GetPaymentIntentByIdQuery(long PaymentIntentId) : IRequest<PaymentIntent?>;
