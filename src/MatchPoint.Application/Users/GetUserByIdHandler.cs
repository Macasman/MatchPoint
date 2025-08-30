using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Users;

public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, User?>
{
    private readonly IUserRepository _repo;
    public GetUserByIdHandler(IUserRepository repo) => _repo = repo;

    public Task<User?> Handle(GetUserByIdQuery req, CancellationToken ct)
        => _repo.GetByIdAsync(req.UserId, ct);
}
