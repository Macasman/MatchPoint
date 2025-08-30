using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces;

public interface IReservationRepository
{
    Task<long> CreateAsync(Reservation entity, CancellationToken ct);
    Task<Reservation?> GetByIdAsync(long id, CancellationToken ct);

    Task<(IReadOnlyList<Reservation> Items, int Total)> ListByUserAsync(
     long userId, DateTime? from, DateTime? to, byte? status, int page, int pageSize, CancellationToken ct);

    Task<(IReadOnlyList<Reservation> Items, int Total)> ListByResourceAsync(
        long resourceId, DateTime? from, DateTime? to, byte? status, int page, int pageSize, CancellationToken ct);

    Task<bool> CancelAsync(long reservationId, CancellationToken ct);
}
