// MatchPoint.Application/Reservations/Commands/CreateReservationCommand.cs
using MatchPoint.Application.Interfaces;
using MatchPoint.Application.Reservations;
using MatchPoint.Domain.Entities;
using MediatR;
using static MatchPoint.Domain.Enums.Enums;
// ...
public sealed class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, long>
{
    private readonly IReservationRepository _repo;
    private readonly IResourceRepository _resources;
    private readonly IPaymentIntentRepository _payments; // 👈

    public CreateReservationCommandHandler(IReservationRepository repo, IResourceRepository resources, IPaymentIntentRepository payments)
    {
        _repo = repo;
        _resources = resources;
        _payments = payments; // 👈
    }

    public async Task<long> Handle(CreateReservationCommand cmd, CancellationToken ct)
    {
        // validações básicas
        if (cmd.StartTime >= cmd.EndTime)
            throw new ArgumentException("StartTime must be < EndTime");
        if (cmd.PriceCents < 0)
            throw new ArgumentException("PriceCents must be >= 0");
        if (!string.Equals(cmd.Currency, "BRL", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only 'BRL' supported for now");

        // recurso ativo?
        var resource = await _resources.GetByIdAsync(cmd.ResourceId, ct);
        if (resource is null || !resource.IsActive)
            throw new InvalidOperationException("Resource not found or inactive");

        // cria reserva
        var reservationId = await _repo.CreateAsync(new Reservation
        {
            UserId = cmd.UserId,
            ResourceId = cmd.ResourceId,
            StartTime = cmd.StartTime,
            EndTime = cmd.EndTime,
            PriceCents = cmd.PriceCents,
            Currency = cmd.Currency.ToUpperInvariant(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            Status = ReservationStatus.Scheduled
        }, ct);

        // cria PaymentIntent acoplado
        _ = await _payments.CreateForReservationAsync(
            reservationId,
            cmd.PriceCents,
            cmd.Currency.ToUpperInvariant(),
            provider: "Simulado",
            ct);

        return reservationId;
    }
}
