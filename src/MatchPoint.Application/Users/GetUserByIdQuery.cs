using MediatR;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Users;

public record GetUserByIdQuery(long UserId) : IRequest<User?>;
