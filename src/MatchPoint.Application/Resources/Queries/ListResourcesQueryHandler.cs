// MatchPoint.Application/Resources/Queries/ListResourcesQuery.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Contracts.Responses;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.Application.Resources.Queries
{
    public sealed record ListResourcesQuery(bool? OnlyActive) : IRequest<IReadOnlyList<ResourceDto>>;

    public sealed class ListResourcesQueryHandler : IRequestHandler<ListResourcesQuery, IReadOnlyList<ResourceDto>>
    {
        private readonly IResourceRepository _repo;
        public ListResourcesQueryHandler(IResourceRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<ResourceDto>> Handle(ListResourcesQuery q, CancellationToken ct)
        {
            var items = await _repo.ListAsync(q.OnlyActive, ct);
            return items
                .Select(e => new ResourceDto(e.ResourceId, e.Name, e.Location, e.PricePerHourCents, e.Currency, e.IsActive, e.CreationDate, e.UpdateDate))
                .ToList();
        }
    }
}
