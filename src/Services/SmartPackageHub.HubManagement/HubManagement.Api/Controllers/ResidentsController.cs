using HubManagement.Application.HubResidents.Commands;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Infrastructures.ActionResults;
using SmartPackageHub.Common.ConfigOptions;

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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> AddResident([FromBody] AddResidentCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            var result = await _mediator.Send(command).ConfigureAwait(false);
            if (result.Conflict)
                return Conflict(new { Message = result.Message });
            if (!result.Succeeded || result.Id is null)
                return BadRequest(new { Message = result.Message });

            // Single "id" only — camelCase JSON would collide if both id and Id were returned.
            return Ok(new { id = result.Id.Value, message = result.Message });
        }

        [HttpGet("getresidentbyid/{residentid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [Authorize(Roles = "Staff")]
        [ProducesResponseType(typeof(ResidentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Envelope), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetResidentById(Guid residentid)
        {
            var result = await _mediator.Send(new GetResidentByIdQuery(residentid)).ConfigureAwait(false);
            return Ok(result);
        }

        [HttpGet("{id:guid}/profile")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ResidentProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetResidentProfile(Guid id)
        {
            var profile = await _mediator.Send(new GetResidentProfileQuery(id)).ConfigureAwait(false);
            if (profile is null)
                return NotFound();

            return Ok(profile);
        }

        [HttpGet("{id:guid}/packages")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<ResidentPackageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetPackages(Guid id)
        {
            var packages = await _mediator.Send(new GetResidentPackagesQuery(id)).ConfigureAwait(false);
            if (packages == null)
                return NotFound();

            return Ok(packages);
        }

        /// <summary>
        /// Package detail for the owning resident.
        /// Section 1: <c>hubArrivalPhoto</c> when an arrival/parcel photo was captured at intake.
        /// Section 2: <c>hubIntakeNote</c> when intake damage exists (note + damage photo).
        /// Section 3: <c>yourReturnRequest</c> when a return request exists.
        /// Sections are independent and never share photo payloads.
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ResidentPackageDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetPackageDetail(Guid id, Guid packageId)
        {
            var detail = await _mediator.Send(new GetResidentPackageDetailQuery(id, packageId)).ConfigureAwait(false);
            if (detail is null)
                return NotFound();

            return Ok(detail);
        }

        /// <summary>
        /// Owner-scoped "Your return request" only — use to refresh return section without reloading full package detail.
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/return")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ResidentReturnRequestDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetPackageReturn(Guid id, Guid packageId)
        {
            var result = await _mediator.Send(new GetResidentPackageReturnQuery(id, packageId)).ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Lightweight refresh of the arrival photo URL after a signed URL expires.
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/arrival-photo-url")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ArrivalPhotoUrlDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetArrivalPhotoUrl(Guid id, Guid packageId)
        {
            var result = await _mediator.Send(new GetResidentArrivalPhotoUrlQuery(id, packageId)).ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Authenticated stream of the arrival photo (local/dev, or when signed URLs are unavailable).
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/arrival-photo")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetArrivalPhoto(Guid id, Guid packageId)
        {
            var result = await _mediator.Send(new GetResidentArrivalPhotoBytesQuery(id, packageId)).ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return File(result.Content.ToArray(), result.ContentType);
        }

        /// <summary>
        /// Lightweight refresh of the damage photo URL after a signed URL expires — avoids reloading full package detail.
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/damage-photo-url")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(DamagePhotoUrlDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetDamagePhotoUrl(Guid id, Guid packageId)
        {
            var result = await _mediator.Send(new GetResidentDamagePhotoUrlQuery(id, packageId)).ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Authenticated stream of the damage photo (local/dev, or when signed URLs are unavailable).
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/damage-photo")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetDamagePhoto(Guid id, Guid packageId)
        {
            var result = await _mediator.Send(new GetResidentDamagePhotoBytesQuery(id, packageId)).ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return File(result.Content.ToArray(), result.ContentType);
        }

        /// <summary>
        /// Refresh a single return-request photo URL after signed URL expiry.
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/return-photos/{photoId:guid}/url")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ReturnPhotoUrlDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetReturnPhotoUrl(Guid id, Guid packageId, Guid photoId)
        {
            var result = await _mediator.Send(new GetResidentReturnPhotoUrlQuery(id, packageId, photoId))
                .ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Authenticated stream of a resident return photo (local/dev, or when signed URLs are unavailable).
        /// </summary>
        [HttpGet("{id:guid}/packages/{packageId:guid}/return-photos/{photoId:guid}")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetReturnPhoto(Guid id, Guid packageId, Guid photoId)
        {
            var result = await _mediator.Send(new GetResidentReturnPhotoBytesQuery(id, packageId, photoId))
                .ConfigureAwait(false);
            if (result is null)
                return NotFound();

            return File(result.Content.ToArray(), result.ContentType);
        }

        [HttpPost("{id:guid}/otp")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult> RequestOtp(Guid id, [FromBody] RequestOtpBody? body, CancellationToken cancellationToken)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _mediator.Send(
                new RequestResidentOtpCommand(id, clientIp, body?.RecaptchaToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.ResidentFound)
                return NotFound();
            if (result.Otp == null)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                Id = result.ResidentId,
                Otp = result.Otp,
                Message = result.Message,
                SmsQueued = result.SmsQueued
            });
        }

        [HttpPost("{id:guid}/verify-otp")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> VerifyOtp(Guid id, [FromBody] VerifyOtpRequest request)
        {
            var result = await _mediator.Send(new VerifyResidentOtpCommand(id, request?.Code ?? string.Empty)).ConfigureAwait(false);
            if (!result.ResidentFound)
                return NotFound();
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("{id:guid}/history")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<DeliveryHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetHistory(
            Guid id,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null)
        {
            if (fromDate is DateOnly from && toDate is DateOnly to && from > to)
            {
                return BadRequest(new { Message = "fromDate cannot be after toDate." });
            }

            var history = await _mediator.Send(
                new GetResidentDeliveryHistoryQuery(id, fromDate, toDate)).ConfigureAwait(false);
            if (history == null)
                return NotFound();

            return Ok(history);
        }

        [HttpGet("{id:guid}/vacationhold")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(VacationHoldDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetResidentVacationHold([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new GetResidentVacationHoldQuery(id)).ConfigureAwait(false);
            if (!result.ResidentFound)
                return NotFound();
            if (result.Hold is null)
                return NoContent();

            return Ok(result.Hold);
        }

        [HttpPost("{id:guid}/vacationhold")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SaveResidentVacationHold([FromRoute] Guid id, [FromBody] VacationHoldRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);
            try
            {
                var result = await _mediator.Send(
                    new SaveResidentVacationHoldCommand(
                        request.SocietyId,
                        id,
                        request.StartDate,
                        request.EndDate,
                        true)).ConfigureAwait(false);
                return Ok(new { result });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{id:guid}/vacationhold/cancel")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> CancelResidentVacationHold([FromRoute] Guid id)
        {
            try
            {
                var result = await _mediator.Send(new CancelResidentVacationHoldCommand(id)).ConfigureAwait(false);
                return Ok(new { result });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
