using HubManagement.Application.PackageHub.Commands;
using HubManagement.Application.PackageHub.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.Types;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/packages")]
    [ApiController]
    public class PackagesController : ControllerBase
    {
        readonly IMediator _mediator;
        public PackagesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PackageDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Create([FromBody] CreatePackageRequest request)
        {
            var result = await _mediator.Send(new CreatePackageCommand(request ?? new CreatePackageRequest())).ConfigureAwait(false);
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.Package);
        }
    }
}
