using System.Threading;
using System.Threading.Tasks;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Interfaces
{
    public interface IResourceRepository
    {
        Task<long> CreateAsync(Resource e, CancellationToken ct);
        Task<Resource?> GetByIdAsync(long id, CancellationToken ct);

        Task<IReadOnlyList<Resource>> ListAsync(bool? onlyActive, CancellationToken ct);

        Task<bool> UpdateAsync(Resource e, CancellationToken ct); // <- novo
    }
}
