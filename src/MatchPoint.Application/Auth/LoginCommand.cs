using MediatR;

namespace MatchPoint.Application.Auth;

public record LoginCommand(string Email, string Password) : IRequest<string>;
