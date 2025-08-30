using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Contracts.Responses;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.Application.Resources.Queries
{
    public sealed record GetResourceByIdQuery(long ResourceId) : IRequest<ResourceDto?>;

    public sealed class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, ResourceDto?>
    {
        private readonly IResourceRepository _repo;

        public GetResourceByIdQueryHandler(IResourceRepository repo)
            => _repo = repo;

        public async Task<ResourceDto?> Handle(GetResourceByIdQuery q, CancellationToken ct)
        {
            var e = await _repo.GetByIdAsync(q.ResourceId, ct);
            if (e is null) return null;

            return new ResourceDto(e.ResourceId, e.Name, e.Location, e.PricePerHourCents, e.Currency, e.IsActive, e.CreationDate, e.UpdateDate);
        }
    }
}
