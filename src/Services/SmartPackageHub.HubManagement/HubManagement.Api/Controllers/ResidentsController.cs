using HubManagement.Application.HubResidents.Commands;
using HubManagement.Application.HubResidents.DTOs;
using HubManagement.Application.HubResidents.Queries;
using SmartPackageHub.Common.Services.Messaging.Abstractions;
using HubManagement.Core.Hubs.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Applications.Abstractions.Repositories;
using SmartPackageHub.Infrastructures.ActionResults;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/residents")]
    [ApiController]
    public class ResidentsController : ControllerBase
    {
        readonly IMediator _mediator;
        readonly ISmsPumpingGuard _smsPumpingGuard;
        readonly IRepository<Resident> _residents;

        public ResidentsController(
            IMediator mediator,
            ISmsPumpingGuard smsPumpingGuard,
            IRepository<Resident> residents)
        {
            _mediator = mediator;
            _smsPumpingGuard = smsPumpingGuard;
            _residents = residents;
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

            return Ok(new { id = result.Id.Value, Id = result.Id.Value, Message = result.Message });
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

        [HttpPost("{id:guid}/otp")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult> RequestOtp(Guid id, [FromBody] RequestOtpBody? body, CancellationToken cancellationToken)
        {
            var resident = await _residents.GetByIdAsync(id).ConfigureAwait(false);
            if (resident is null)
                return NotFound();

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var phone = resident.PhoneNumber ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var pumping = await _smsPumpingGuard.CheckAsync(
                    clientIp,
                    phone,
                    body?.RecaptchaToken,
                    cancellationToken).ConfigureAwait(false);
                if (!pumping.Allowed)
                    return BadRequest(new { Message = pumping.Reason });
            }

            var result = await _mediator.Send(
                new RequestResidentOtpCommand(id, clientIp, body?.RecaptchaToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.ResidentFound)
                return NotFound();
            if (result.Otp == null)
                return BadRequest(new { Message = result.Message });

            if (result.SmsQueued && !string.IsNullOrWhiteSpace(phone))
                await _smsPumpingGuard.RecordSuccessAsync(clientIp, phone, cancellationToken).ConfigureAwait(false);

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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetHistory(Guid id)
        {
            var history = await _mediator.Send(new GetResidentDeliveryHistoryQuery(id)).ConfigureAwait(false);
            if (history == null)
                return NotFound();

            return Ok(history);
        }

        [HttpPost("{id:guid}/vacationhold")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SaveResidentVacationHold([FromRoute] Guid id, [FromBody] VacationHoldRequestDto request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator.Send(new SaveResidentVacationHoldCommand(request.SocietyId, id,request.StartDate,request.EndDate, true)).ConfigureAwait(false);
            return Ok(new { result });
        }
    }

    
}
