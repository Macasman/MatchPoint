using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Kyc;

public class GetLatestKycByUserHandler : IRequestHandler<GetLatestKycByUserQuery, KycVerification?>
{
    private readonly IKycVerificationRepository _repo;
    public GetLatestKycByUserHandler(IKycVerificationRepository repo) => _repo = repo;

    public Task<KycVerification?> Handle(GetLatestKycByUserQuery req, CancellationToken ct)
        => _repo.GetLatestByUserAsync(req.UserId, ct);
}
