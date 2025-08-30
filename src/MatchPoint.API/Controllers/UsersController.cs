using MediatR;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Users;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    public UsersController(IMediator mediator) => _mediator = mediator;

    public record CreateUserDto(
        string Name,
        string Email,
        string? Phone,
        string? DocumentId,
        DateTime? BirthDate,
        string Password
    );


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateUserCommand(
            dto.Name, dto.Email, dto.Phone, dto.DocumentId, dto.BirthDate, dto.Password
        ), ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id), ct);
        return user is null ? NotFound() : Ok(user);
    }
}
