using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces;

public interface IAuditRepository
{
    Task LogAsync(AuditEvent e, CancellationToken ct);
}
