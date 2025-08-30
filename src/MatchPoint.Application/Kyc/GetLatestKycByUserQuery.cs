using MediatR;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Kyc;

public record GetLatestKycByUserQuery(long UserId) : IRequest<KycVerification?>;
