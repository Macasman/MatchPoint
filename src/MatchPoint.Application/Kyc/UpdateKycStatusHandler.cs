using MediatR;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.Application.Kyc;

public class UpdateKycStatusHandler : IRequestHandler<UpdateKycStatusCommand, bool>
{
    private readonly IKycVerificationRepository _repo;
    public UpdateKycStatusHandler(IKycVerificationRepository repo) => _repo = repo;

    public Task<bool> Handle(UpdateKycStatusCommand req, CancellationToken ct)
    {
        if (req.Status is > 3) throw new ArgumentOutOfRangeException(nameof(req.Status), "Status inválido (0..3).");
        if (req.Score is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(req.Score), "Score deve estar entre 0 e 100.");
        return _repo.UpdateStatusAsync(req.KycId, req.Status, req.Score, req.Notes, ct);
    }
}
