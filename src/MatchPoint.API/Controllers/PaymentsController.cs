using MediatR;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Payments;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.API.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    public record CreatePaymentIntentDto(long ReservationId, int AmountCents, string Currency = "BRL", string? Provider = "Simulado");

    [HttpPost("intents")]
    public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentDto dto, CancellationToken ct)
    {
        var id = await _mediator.Send(new CreatePaymentIntentCommand(dto.ReservationId, dto.AmountCents, dto.Currency, dto.Provider), ct);
        return CreatedAtAction(nameof(GetIntent), new { id }, new { id });
    }

    [HttpGet("intents/{id:long}")]
    public async Task<IActionResult> GetIntent([FromRoute] long id, CancellationToken ct)
    {
        var pi = await _mediator.Send(new GetPaymentIntentByIdQuery(id), ct);
        return pi is null ? NotFound() : Ok(pi);
    }

    [HttpPost("intents/{id:long}/capture")]
    public async Task<IActionResult> Capture([FromRoute] long id, CancellationToken ct)
    {
        var ok = await _mediator.Send(new CapturePaymentIntentCommand(id), ct);
        return ok ? Ok(new { captured = true, id }) : BadRequest(new { captured = false, id });
    }

    [HttpPost("intents/{id:long}/enqueue-capture")]
    public async Task<IActionResult> EnqueueCapture([FromRoute] long id, [FromServices] IWebhookQueueWriter queue, CancellationToken ct)
    {
        var jobId = await queue.EnqueuePaymentEventAsync(id, "payment.captured", "manual", DateTime.UtcNow, ct);
        return Accepted(new { jobId, paymentIntentId = id, enqueued = true });
    }
}
