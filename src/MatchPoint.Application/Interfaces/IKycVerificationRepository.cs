using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces;

public interface IKycVerificationRepository
{
    Task<long> CreateAsync(KycVerification entity, CancellationToken ct);
    Task<KycVerification?> GetByIdAsync(long kycId, CancellationToken ct);
    Task<KycVerification?> GetLatestByUserAsync(long userId, CancellationToken ct);
    Task<bool> UpdateStatusAsync(long kycId, byte status, decimal? score, string? notes, CancellationToken ct);
}
