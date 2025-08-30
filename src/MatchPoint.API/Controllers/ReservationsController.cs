using MediatR;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Reservations;
using Microsoft.AspNetCore.Authorization;
using static MatchPoint.Domain.Enums.Enums;

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

    [HttpGet("users/{userId:long}")]
    public async Task<IActionResult> ListByUser(
        long userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ReservationStatus? status,   // 👈 enum aqui
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListReservationsByUserQuery(userId, from, to, status, page, pageSize), ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("resources/{resourceId:long}")]
    public async Task<IActionResult> ListByResource(
      long resourceId,
      [FromQuery] DateTime? from,
      [FromQuery] DateTime? to,
      [FromQuery] ReservationStatus? status,   // <- aceita ?status=1 ou ?status=Agendada
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 20,
      CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(
            new ListReservationsByResourceQuery(resourceId, from, to, status, page, pageSize), ct);
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize] // se aplicável
    public async Task<IActionResult> Cancel([FromRoute] long id, CancellationToken ct)
    {
        var canceled = await _mediator.Send(new CancelReservationCommand(id), ct);

        return Ok(new { canceled });
    }
}
