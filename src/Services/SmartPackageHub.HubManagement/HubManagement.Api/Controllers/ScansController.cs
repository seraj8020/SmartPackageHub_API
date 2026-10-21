using HubManagement.Application.Scans.Commands;
using HubManagement.Application.Scans.DTOs;
using HubManagement.Application.Scans.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.Types;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/scans")]
    [ApiController]
    public class ScansController : ControllerBase
    {
        readonly IMediator _mediator;
        public ScansController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("receive")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> ScanAndReceive([FromBody] ScanReceiveRequest request)
        {
            var result = await _mediator.Send(new ScanReceiveCommand(request ?? new ScanReceiveRequest())).ConfigureAwait(false);
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                Message = result.Message,
                Package = new
                {
                    result.Package!.Id,
                    result.Package.PkgNumber,
                    result.Package.TrackingNumber,
                    result.Package.ReceivedAt,
                    result.Package.Courier,
                    result.Package.ResidentId
                }
            });
        }

        [HttpPost("handover")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ScanAndHandover([FromBody] ScanHandoverRequest request)
        {
            var result = await _mediator.Send(new ScanHandoverCommand(request ?? new ScanHandoverRequest())).ConfigureAwait(false);
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                Message = result.Message,
                Package = new
                {
                    result.Package!.Id,
                    result.Package.PkgNumber,
                    result.Package.TrackingNumber
                }
            });
        }

        [HttpGet("parse")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ScanParseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Parse([FromQuery] string? vendor, [FromQuery] string? code)
        {
            var parsed = await _mediator.Send(new ParseScanQuery(vendor, code)).ConfigureAwait(false);
            if (parsed == null)
                return BadRequest();

            return Ok(parsed);
        }
    }
}
