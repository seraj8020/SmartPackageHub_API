using HubManagement.Application.Auth.Commands;
using HubManagement.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Common.Types;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/auth")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Resident login step 1: look up Residents by mobile and issue a short-lived OTP.
        /// In Development the OTP is returned in the response for local testing.
        /// </summary>
        [HttpPost("resident/otp/request")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RequestResidentOtp(
            [FromBody] RequestResidentLoginOtpRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new RequestResidentLoginOtpCommand(request.PhoneNumber), cancellationToken)
                .ConfigureAwait(false);

            if (result.NotFound)
                return NotFound(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                result.ResidentId,
                result.Otp,
                Message = result.Message
            });
        }

        /// <summary>
        /// Resident login step 2: verify OTP and return opaque session token + resident id.
        /// </summary>
        [HttpPost("resident/otp/verify")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> VerifyResidentOtp(
            [FromBody] VerifyResidentLoginOtpRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new VerifyResidentLoginOtpCommand(request.PhoneNumber, request.Otp), cancellationToken)
                .ConfigureAwait(false);

            if (result.NotFound)
                return NotFound(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                result.Token,
                result.ResidentId,
                result.SocietyId,
                result.FullName,
                result.PhoneNumber,
                result.Role,
                Message = result.Message
            });
        }

        /// <summary>
        /// Staff / admin login with username + password. Returns user id and linked society ids.
        /// </summary>
        [HttpPost("staff/login")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> StaffLogin(
            [FromBody] StaffLoginRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new StaffLoginCommand(request.Username, request.Password), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
                return Unauthorized(new { Message = result.Message });

            return Ok(new
            {
                result.Token,
                result.UserId,
                result.Username,
                result.Role,
                SocietyIds = result.SocietyIds,
                result.PrimarySocietyId,
                Message = result.Message
            });
        }
    }
}
