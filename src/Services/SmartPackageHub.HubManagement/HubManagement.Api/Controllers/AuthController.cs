using HubManagement.Application.Auth.Commands;
using HubManagement.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartPackageHub.Common.ConfigOptions;

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

        /// <summary>
        /// Admin login (platform or society): username/password. On success, issues an access token.
        /// </summary>
        [HttpPost("platform/login")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> PlatformAdminLogin(
            [FromBody] PlatformAdminLoginRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new PlatformAdminLoginCommand(request.Username, request.Password), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
            {
                if (string.Equals(result.Role, "Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        Message = result.Message,
                        Code = "StaffAdminPortalDenied",
                        Role = result.Role
                    });
                }

                return Unauthorized(new { Message = result.Message });
            }

            return Ok(new
            {
                result.MfaRequired,
                result.MfaToken,
                result.AccessToken,
                result.EmailHint,
                result.DevelopmentCode,
                result.Role,
                result.Username,
                SocietyIds = result.SocietyIds ?? Array.Empty<Guid>(),
                Message = result.Message
            });
        }

        /// <summary>
        /// Platform Admin MFA verify (legacy / optional). Prefer password login which issues AccessToken directly.
        /// </summary>
        [HttpPost("platform/mfa/verify")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> VerifyPlatformAdminMfa(
            [FromBody] VerifyPlatformAdminMfaRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new VerifyPlatformAdminMfaCommand(request.MfaToken, request.Code), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                result.AccessToken,
                result.EmailHint,
                result.Role,
                result.Username,
                SocietyIds = result.SocietyIds ?? Array.Empty<Guid>(),
                Message = result.Message
            });
        }

        /// <summary>
        /// Admin change password (requires Bearer access token from login).
        /// </summary>
        [HttpPost("platform/password/change")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> ChangePlatformAdminPassword(
            [FromBody] ChangePlatformAdminPasswordRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var auth = Request.Headers.Authorization.ToString();
            var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? auth["Bearer ".Length..].Trim()
                : null;

            var result = await _mediator
                .Send(
                    new ChangePlatformAdminPasswordCommand(token, request.CurrentPassword, request.NewPassword),
                    cancellationToken)
                .ConfigureAwait(false);

            if (result.Unauthorized)
                return Unauthorized(new { Message = result.Message });
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        /// <summary>
        /// Platform Admin forgot password step 1: send email reset code.
        /// </summary>
        [HttpPost("platform/password/forgot")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ForgotPlatformAdminPassword(
            [FromBody] ForgotPlatformAdminPasswordRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new ForgotPlatformAdminPasswordCommand(request.UsernameOrEmail), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                result.ResetToken,
                result.EmailHint,
                result.DevelopmentCode,
                Message = result.Message
            });
        }

        /// <summary>
        /// Platform Admin forgot password step 2: verify email code and set new password.
        /// </summary>
        [HttpPost("platform/password/reset")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ResetPlatformAdminPassword(
            [FromBody] ResetPlatformAdminPasswordRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(
                    new ResetPlatformAdminPasswordCommand(request.ResetToken, request.Code, request.NewPassword),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        /// <summary>
        /// Platform Admin forgot username: email the username to the registered address.
        /// </summary>
        [HttpPost("platform/username/forgot")]
        [MapToApiVersion(ApiVersions.V2)]
        [EnableRateLimiting("otp-sms")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ForgotPlatformAdminUsername(
            [FromBody] ForgotPlatformAdminUsernameRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            var result = await _mediator
                .Send(new ForgotPlatformAdminUsernameCommand(request.Email), cancellationToken)
                .ConfigureAwait(false);

            if (!result.Succeeded)
                return BadRequest(new { Message = result.Message });

            return Ok(new
            {
                result.Username,
                result.EmailHint,
                Message = result.Message
            });
        }
    }
}
