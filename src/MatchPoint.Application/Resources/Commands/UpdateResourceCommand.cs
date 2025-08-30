using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Contracts.Requests;
using MatchPoint.Application.Interfaces;

namespace MatchPoint.Application.Resources.Commands
{
    public sealed record UpdateResourceCommand(long ResourceId, UpdateResourceRequest Request) : IRequest<bool>;

    public sealed class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, bool>
    {
        private readonly IResourceRepository _repo;

        public UpdateResourceCommandHandler(IResourceRepository repo)
            => _repo = repo;

        public async Task<bool> Handle(UpdateResourceCommand cmd, CancellationToken ct)
        {
            var r = cmd.Request;

            if (string.IsNullOrWhiteSpace(r.Name))
                throw new ArgumentException("Name is required");
            if (r.PricePerHourCents < 0)
                throw new ArgumentException("PricePerHourCents must be >= 0");

            var existing = await _repo.GetByIdAsync(cmd.ResourceId, ct);
            if (existing is null) return false;

            existing.Name = r.Name.Trim();
            existing.Location = string.IsNullOrWhiteSpace(r.Location) ? null : r.Location.Trim();
            existing.PricePerHourCents = r.PricePerHourCents;
            existing.IsActive = r.IsActive;
            existing.UpdateDate = System.DateTime.UtcNow;

            return await _repo.UpdateAsync(existing, ct);
        }
    }
}
