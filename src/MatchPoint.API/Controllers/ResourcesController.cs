using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MatchPoint.Application.Contracts.Requests;
using MatchPoint.Application.Resources.Commands;
using MatchPoint.Application.Resources.Queries;

namespace MatchPoint.API.Controllers
{
    [ApiController]
    [Route("resources")]
    public class ResourcesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ResourcesController(IMediator mediator) => _mediator = mediator;

        /// <summary>Create a new resource.</summary>
        [HttpPost]
        [Authorize] // TODO: restrict to admin when roles are available
        public async Task<IActionResult> Create([FromBody] CreateResourceRequest body, CancellationToken ct)
        {
            var id = await _mediator.Send(new CreateResourceCommand(body), ct);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        /// <summary>Get a resource by id.</summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
        {
            var dto = await _mediator.Send(new GetResourceByIdQuery(id), ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>List resources optionally filtering by active flag.</summary>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool? active, CancellationToken ct)
        {
            var list = await _mediator.Send(new ListResourcesQuery(active), ct);
            return Ok(list);
        }

        /// <summary>Update resource (name, location, price, active).</summary>
        [HttpPatch("{id:long}")]
        [Authorize] // TODO: restrict to admin when roles are available
        public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateResourceRequest body, CancellationToken ct)
        {
            var ok = await _mediator.Send(new UpdateResourceCommand(id, body), ct);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
