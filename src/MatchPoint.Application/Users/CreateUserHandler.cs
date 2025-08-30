using MediatR;
using MatchPoint.Application.Users;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace MatchPoint.Application.Users;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, long>
{
    private readonly IUserRepository _repo;
    private readonly IAuditRepository _audit;

    public CreateUserHandler(IUserRepository repo, IAuditRepository audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<long> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var entity = new User
        {
            Name = req.Name.Trim(),
            Email = req.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim(),
            DocumentId = string.IsNullOrWhiteSpace(req.DocumentId) ? null : req.DocumentId.Trim(),
            BirthDate = req.BirthDate,
            PasswordHash = ComputeHash(req.Password), // 👈 gera hash
            IsActive = true,
            CreationDate = DateTime.UtcNow
        };

        var id = await _repo.CreateAsync(entity, ct);

        await _audit.LogAsync(new AuditEvent
        {
            Aggregate = "User",
            AggregateId = id,
            Action = "Create",
            Data = $"User {entity.Email} created",
            UserId = null
        }, ct);

        return id;
    }

    private static string ComputeHash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
