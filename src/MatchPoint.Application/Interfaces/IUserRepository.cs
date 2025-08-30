using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces;

public interface IUserRepository
{
    Task<long> CreateAsync(User entity, CancellationToken ct);
    Task<User?> GetByIdAsync(long id, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
}
