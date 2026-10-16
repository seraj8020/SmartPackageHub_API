using HubManagement.Application.HubResidents.Commands;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.Types;
using SmartPackageHub.Infrastructures.ActionResults;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/residents")]
    [ApiController]
    public class ResidentsController : ControllerBase
    {
        readonly IMediator _mediator;
        public ResidentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("addresident")]
        [MapToApiVersion(ApiVersions.V2)]
        //[Authorize(Roles = "Resident")]
        [ProducesResponseType(typeof(CreatedResultEnvelope), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> AddResident([FromBody] AddResidentCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet("getresidentbyid/{residentid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(ResidentDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetResidentById(Guid residentid)
        {
            var result = await _mediator.Send(new GetResidentByIdQuery(residentid));
            return Ok(result);
        }

        [HttpGet("{id:guid}/packages")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<ResidentPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetPackages(Guid id)
        {
            var packages = await _mediator.Send(new GetResidentPackagesQuery(id));
            if (packages == null)
                return NotFound();

            return Ok(packages);
        }

        [HttpPost("{id:guid}/otp")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RequestOtp(Guid id)
        {
            var result = await _mediator.Send(new RequestResidentOtpCommand(id));
            if (!result.ResidentFound)
                return NotFound();
            if (result.Otp == null)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Id = result.ResidentId, Otp = result.Otp, Message = result.Message });
        }

        [HttpPost("{id:guid}/verify-otp")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> VerifyOtp(Guid id, [FromBody] VerifyOtpRequest request)
        {
            var result = await _mediator.Send(new VerifyResidentOtpCommand(id, request?.Code ?? string.Empty));
            if (!result.ResidentFound)
                return NotFound();
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("{id:guid}/history")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<DeliveryHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetHistory(Guid id)
        {
            var history = await _mediator.Send(new GetResidentDeliveryHistoryQuery(id));
            if (history == null)
                return NotFound();

            return Ok(history);
        }
    }
}
