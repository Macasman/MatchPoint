using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MatchPoint.Application.Contracts.Requests;
using MatchPoint.Application.Interfaces;
using MatchPoint.Domain.Entities;

namespace MatchPoint.Application.Resources.Commands
{
    public sealed record CreateResourceCommand(CreateResourceRequest Request) : IRequest<long>;

    public sealed class CreateResourceCommandHandler : IRequestHandler<CreateResourceCommand, long>
    {
        private readonly IResourceRepository _repo;

        public CreateResourceCommandHandler(IResourceRepository repo)
            => _repo = repo;

        public async Task<long> Handle(CreateResourceCommand cmd, CancellationToken ct)
        {
            var r = cmd.Request;

            // Validations
            if (string.IsNullOrWhiteSpace(r.Name))
                throw new ArgumentException("Name is required");
            if (r.PricePerHourCents < 0)
                throw new ArgumentException("PricePerHourCents must be >= 0");
            if (!string.Equals(r.Currency, "BRL", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only 'BRL' is supported at the moment");

            var entity = new Resource
            {
                Name = r.Name.Trim(),
                Location = string.IsNullOrWhiteSpace(r.Location) ? null : r.Location.Trim(),
                PricePerHourCents = r.PricePerHourCents,
                Currency = r.Currency.ToUpperInvariant(),
                IsActive = r.IsActive
            };

            return await _repo.CreateAsync(entity, ct);
        }
    }
}
