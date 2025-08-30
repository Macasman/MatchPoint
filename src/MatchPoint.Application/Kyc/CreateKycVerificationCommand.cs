using MediatR;

namespace MatchPoint.Application.Kyc;

public record CreateKycVerificationCommand(
    long UserId,
    string? Provider = "Simulado",
    decimal? Score = null,
    string? Notes = null
) : IRequest<long>;
