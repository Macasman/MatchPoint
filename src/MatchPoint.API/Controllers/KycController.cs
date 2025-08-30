using MediatR;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Kyc;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("kyc")]
public class KycController : ControllerBase
{
    private readonly IMediator _mediator;
    public KycController(IMediator mediator) => _mediator = mediator;

    public record CreateKycDto(long UserId, string? Provider = "Simulado", decimal? Score = null, string? Notes = null);
    public record UpdateKycDto(byte Status, decimal? Score = null, string? Notes = null);

    // POST /kyc/verifications
    [HttpPost("verifications")]
    public async Task<IActionResult> Create([FromBody] CreateKycDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreateKycVerificationCommand(dto.UserId, dto.Provider, dto.Score, dto.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    // GET /kyc/verifications/{id}
    [HttpGet("verifications/{id:long}")]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var item = await _mediator.Send(new GetKycByIdQuery(id), ct);
        return item is null ? NotFound() : Ok(item);
    }

    // GET /kyc/users/{userId}/latest
    [HttpGet("users/{userId:long}/latest")]
    public async Task<IActionResult> GetLatestByUser([FromRoute] long userId, CancellationToken ct)
    {
        var item = await _mediator.Send(new GetLatestKycByUserQuery(userId), ct);
        return item is null ? NotFound() : Ok(item);
    }

    // PATCH /kyc/verifications/{id}
    [HttpPatch("verifications/{id:long}")]
    public async Task<IActionResult> UpdateStatus([FromRoute] long id, [FromBody] UpdateKycDto dto, CancellationToken ct)
    {
        var ok = await _mediator.Send(new UpdateKycStatusCommand(id, dto.Status, dto.Score, dto.Notes), ct);
        return ok ? Ok(new { updated = true, id }) : NotFound(new { updated = false, id });
    }
}
