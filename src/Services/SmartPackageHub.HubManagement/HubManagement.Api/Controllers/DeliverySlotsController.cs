using HubManagement.Application.DeliverySlots.Commands;
using HubManagement.Application.DeliverySlots.DTOs;
using HubManagement.Application.DeliverySlots.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.Types;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/delivery-slots")]
    [ApiController]
    public class DeliverySlotsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DeliverySlotsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("availability")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(DeliverySlotAvailabilityDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> GetAvailability(
            [FromQuery] Guid societyId,
            [FromQuery] DateOnly? date)
        {
            if (societyId == Guid.Empty)
                return BadRequest(new { Message = "societyId is required." });

            var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _mediator
                .Send(new GetDeliverySlotAvailabilityQuery(societyId, day))
                .ConfigureAwait(false);
            return Ok(result);
        }

        [HttpPost]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(DeliverySlotDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Book([FromBody] BookDeliverySlotRequest request)
        {
            var result = await _mediator
                .Send(new BookDeliverySlotCommand(request ?? new BookDeliverySlotRequest()))
                .ConfigureAwait(false);

            if (!result.Found)
                return NotFound(new { Message = result.Message });
            if (result.Conflict)
                return Conflict(new { Message = result.Message, Slot = result.Slot });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(result.Slot);
        }
    }
}
