using MediatR;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Reservations;
using Microsoft.AspNetCore.Authorization;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReservationsController(IMediator mediator) => _mediator = mediator;

    public record CreateReservationDto(long UserId, long ResourceId, DateTime StartTime, DateTime EndTime, int PriceCents, string Currency = "BRL", string? Notes = null);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateReservationCommand(
            dto.UserId, dto.ResourceId, dto.StartTime, dto.EndTime, dto.PriceCents, dto.Currency, dto.Notes
        ), ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetReservationByIdQuery(id), ct);
        return res is null ? NotFound() : Ok(res);
    }
}
