using MediatR;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Auth;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    public record LoginDto(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var token = await _mediator.Send(new LoginCommand(dto.Email, dto.Password), ct);
        return Ok(new { token });
    }
}
