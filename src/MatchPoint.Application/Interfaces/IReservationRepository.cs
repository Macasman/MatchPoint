using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces;

public interface IReservationRepository
{
    Task<long> CreateAsync(Reservation entity, CancellationToken ct);
    Task<Reservation?> GetByIdAsync(long id, CancellationToken ct);
}
