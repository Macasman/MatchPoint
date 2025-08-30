using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Kyc;

public class GetKycByIdHandler : IRequestHandler<GetKycByIdQuery, KycVerification?>
{
    private readonly IKycVerificationRepository _repo;
    public GetKycByIdHandler(IKycVerificationRepository repo) => _repo = repo;

    public Task<KycVerification?> Handle(GetKycByIdQuery req, CancellationToken ct)
        => _repo.GetByIdAsync(req.KycId, ct);
}
