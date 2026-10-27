using HubManagement.Application.Intake.DTOs;
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

        [HttpPut("{packageId:guid}/dispatch")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DispatchPackages(Guid packageId)
        {
            var result = await _mediator.Send(new DispatchPackageCommand(packageId)).ConfigureAwait(false);
            return Ok(result);
        }

        [HttpPost("{packageId:guid}/deliver")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> DeliverPackages([FromRoute] Guid packageId, [FromBody] DeliverPackageRequest request)
        {
            var result = await _mediator.Send(new DeliverPackageCommand(packageId, request)).ConfigureAwait(false);
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            // Slim success body — staff UI only needs succeeded + message.
            return Ok(new { succeeded = true, message = result.Message });
        }

        [HttpPost("{packageId:guid}/cod/approve")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> ApproveCod(Guid packageId)
        {
            var result = await _mediator.Send(new RespondCodCommand(packageId, Approve: true)).ConfigureAwait(false);
            return MapCodResult(result);
        }

        [HttpPost("{packageId:guid}/cod/reject")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RejectCod(Guid packageId)
        {
            var result = await _mediator.Send(new RespondCodCommand(packageId, Approve: false)).ConfigureAwait(false);
            return MapCodResult(result);
        }

        [HttpPost("{packageId:guid}/shelf")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ConfirmIntakeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AssignShelf(Guid packageId, [FromBody] AssignShelfRequest? request)
        {
            var result = await _mediator.Send(new AssignShelfCommand(
                packageId,
                request?.ShelfCode,
                request?.IsColdStorage ?? false)).ConfigureAwait(false);

            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Conflict)
                return Conflict(new { succeeded = false, message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            return Ok(result.Receipt);
        }

        private ActionResult MapCodResult(CodResponseResult result)
        {
            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Conflict)
                return Conflict(new
                {
                    succeeded = false,
                    message = result.Message,
                    status = result.Status,
                    codApprovalStatus = result.CodApprovalStatus,
                });
            if (!result.Succeeded)
                return BadRequest(new { succeeded = false, message = result.Message });

            return Ok(new
            {
                succeeded = true,
                message = result.Message,
                pickupOtp = result.PickupOtp,
                status = result.Status,
                codApprovalStatus = result.CodApprovalStatus,
            });
        }
    }
}
