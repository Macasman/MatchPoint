using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Kyc;

public class CreateKycVerificationHandler : IRequestHandler<CreateKycVerificationCommand, long>
{
    private readonly IKycVerificationRepository _repo;
    public CreateKycVerificationHandler(IKycVerificationRepository repo) => _repo = repo;

    public Task<long> Handle(CreateKycVerificationCommand req, CancellationToken ct)
    {
        if (req.Score is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(req.Score), "Score deve estar entre 0 e 100.");
        var entity = new KycVerification
        {
            UserId = req.UserId,
            Provider = string.IsNullOrWhiteSpace(req.Provider) ? "Simulado" : req.Provider,
            Score = req.Score,
            Status = 0, // Pendente
            Notes = req.Notes
        };
        return _repo.CreateAsync(entity, ct);
    }
}
