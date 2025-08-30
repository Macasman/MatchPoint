using MediatR;

namespace MatchPoint.Application.Users;

public record CreateUserCommand(
    string Name,
    string Email,
    string? Phone,
    string? DocumentId,
    DateTime? BirthDate,
    string Password
) : IRequest<long>;