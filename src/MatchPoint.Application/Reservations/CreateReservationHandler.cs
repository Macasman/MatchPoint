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

    public CreateReservationCommandHandler(
        IReservationRepository repo,
        IResourceRepository resources,
        IPaymentIntentRepository payments)
    {
        _repo = repo;
        _resources = resources;
        _payments = payments;
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

        // Garante UM PI aberto por reserva: capturar exceção de unique index (ver DDL abaixo)
        try
        {
            _ = await _payments.CreateForReservationAsync(
                reservationId, cmd.PriceCents, entity.Currency, "Simulado", ct);
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            // Já existe PI aberto; tudo bem seguir
        }

        return reservationId;
    }
}
