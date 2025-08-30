using MediatR;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Kyc;

public record GetKycByIdQuery(long KycId) : IRequest<KycVerification?>;
