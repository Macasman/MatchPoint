using MediatR;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;
using static MatchPoint.Domain.Enums.Enums;
using System.Data.SqlClient;

namespace MatchPoint.Application.Reservations;

public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, long>
{
    private readonly IReservationRepository _repo;
    private readonly IResourceRepository _resources;
    private readonly IPaymentIntentRepository _payments;
    private readonly IWebhookQueueWriter _webhookQueue; // 👈 novo

    public CreateReservationCommandHandler(
        IReservationRepository repo,
        IResourceRepository resources,
        IPaymentIntentRepository payments,
        IWebhookQueueWriter webhookQueue)
    {
        _repo = repo;
        _resources = resources;
        _payments = payments;
        _webhookQueue = webhookQueue;
    }

    public async Task<long> Handle(CreateReservationCommand cmd, CancellationToken ct)
    {
        if (cmd.StartTime >= cmd.EndTime) throw new ArgumentException("StartTime must be < EndTime");
        if (cmd.PriceCents < 0) throw new ArgumentException("PriceCents must be >= 0");
        if (!string.Equals(cmd.Currency, "BRL", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only 'BRL' supported for now");

        var resource = await _resources.GetByIdAsync(cmd.ResourceId, ct);
        if (resource is null || !resource.IsActive) throw new InvalidOperationException("Resource not found or inactive");

        var entity = new Reservation
        {
            UserId = cmd.UserId,
            ResourceId = cmd.ResourceId,
            StartTime = cmd.StartTime,
            EndTime = cmd.EndTime,
            PriceCents = cmd.PriceCents,
            Currency = cmd.Currency.ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            Status = ReservationStatus.Scheduled
        };

        var (created, reservationId) = await _repo.CreateIfNoOverlapAsync(entity, ct);
        if (!created) return 0;

        long paymentIntentId = 0;
        try
        {
            paymentIntentId = await _payments.CreateForReservationAsync(
                reservationId, cmd.PriceCents, entity.Currency, "Simulado", ct) ?? 0L;
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            // PI já existe -> segue fluxo; poderíamos buscar o existente se precisar do id
        }

        // 👇 Enfileira o "capture" simulado (worker vai postar no webhook da API)
        if (paymentIntentId > 0)
        {
            await _webhookQueue.EnqueuePaymentEventAsync(
                paymentIntentId,
                @event: "payment.captured",
                providerRef: "sim-auto",
                scheduleUtc: DateTime.UtcNow.AddSeconds(2),
                ct: ct);
        }

        return reservationId;
    }
}
