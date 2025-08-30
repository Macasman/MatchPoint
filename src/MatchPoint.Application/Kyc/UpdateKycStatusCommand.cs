using MediatR;

namespace MatchPoint.Application.Kyc;

public record UpdateKycStatusCommand(long KycId, byte Status, decimal? Score = null, string? Notes = null) : IRequest<bool>;
