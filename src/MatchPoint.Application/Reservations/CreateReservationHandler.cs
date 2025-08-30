using MediatR;
using MatchPoint.Domain.Entities;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.Application.Reservations;

public class CreateReservationHandler : IRequestHandler<CreateReservationCommand, long>
{
    private readonly IReservationRepository _repo;
    private readonly IAuditRepository _audit;
    private readonly IEventPublisher _publisher;

    public CreateReservationHandler(IReservationRepository repo, IAuditRepository audit, IEventPublisher publisher)
    {
        _repo = repo;
        _audit = audit;
        _publisher = publisher;
    }

    public async Task<long> Handle(CreateReservationCommand req, CancellationToken ct)
    {
        var id = await _repo.CreateAsync(new Reservation
        {
            UserId = req.UserId,
            ResourceId = req.ResourceId,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            Status = 1,
            PriceCents = req.PriceCents,
            Currency = req.Currency,
            Notes = req.Notes
        }, ct);


        await _audit.LogAsync(new AuditEvent
        {
            Aggregate = "Reservation",
            AggregateId = id,
            Action = "Created",
            Data = $"{{ \"UserId\": {req.UserId}, \"ResourceId\": {req.ResourceId} }}",
            UserId = req.UserId
        }, ct);

        var evt = new MatchPoint.Application.Events.ReservationCreatedEvent
        {
            ReservationId = id,
            UserId = req.UserId,
            ResourceId = req.ResourceId,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            PriceCents = req.PriceCents,
            Currency = string.IsNullOrWhiteSpace(req.Currency) ? "BRL" : req.Currency
        };
        await _publisher.PublishAsync("reservations.created", evt, ct);

        return id;
    }
}
