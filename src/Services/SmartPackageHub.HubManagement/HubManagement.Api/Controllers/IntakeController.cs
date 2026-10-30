using HubManagement.Application.Intake.Commands;
using HubManagement.Application.Intake.DTOs;
using HubManagement.Application.Intake.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SmartPackageHub.Common.ConfigOptions;

namespace SmartPackageHub.HubManagement.Api.Controllers
{
    [Route("api/v{version:apiVersion}/intake")]
    [ApiController]
    public class IntakeController : ControllerBase
    {
        readonly IMediator _mediator;
        public IntakeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("parse")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ParsedLabelDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Parse([FromBody] ParseLabelRequest request)
        {
            var parsed = await _mediator.Send(new ParseLabelQuery(request?.Text)).ConfigureAwait(false);
            if (parsed == null)
                return BadRequest(new { Message = "Text is required" });

            return Ok(parsed);
        }

        [HttpGet("match")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult> Match(
            [FromQuery] string? tower,
            [FromQuery] string? flat,
            [FromQuery] string? name,
            [FromQuery] Guid? societyId)
        {
            return Ok(await _mediator.Send(new MatchResidentsQuery(tower, flat, name, societyId)).ConfigureAwait(false));
        }

        [HttpGet("residents")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<ResidentMatchDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Residents([FromQuery] string? q, [FromQuery] Guid? societyId)
        {
            return Ok(await _mediator.Send(new SearchResidentsQuery(q, societyId)).ConfigureAwait(false));
        }

        [HttpGet("shelf")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ShelfResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Shelf([FromQuery] Guid? societyId)
        {
            var shelf = await _mediator.Send(new SuggestShelfQuery(societyId)).ConfigureAwait(false);
            if (shelf == null)
                return NotFound(new { Message = "No shelf is available" });

            return Ok(shelf);
        }

        [HttpPost("confirm")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ConfirmIntakeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Confirm([FromBody] ConfirmIntakeRequest request)
        {
            var result = await _mediator.Send(new ConfirmIntakeCommand(request ?? new ConfirmIntakeRequest())).ConfigureAwait(false);
            if (result.StatusCode == 400)
                return BadRequest(new { Message = result.Error });
            if (result.StatusCode == 404)
                return NotFound(new { Message = result.Error });
            if (result.StatusCode == 409)
                return Conflict(new { Message = result.Error });

            return Ok(result.Body);
        }

        [HttpPost("photos")]
        [MapToApiVersion(ApiVersions.V2)]
        [RequestSizeLimit(2_000_000)]
        [ProducesResponseType(typeof(PhotoUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Photo([FromForm] string? clientPhotoId, IFormFile? file)
        {
            if (string.IsNullOrWhiteSpace(clientPhotoId) || file == null || file.Length == 0)
                return BadRequest(new { Message = "A photo id and image are required" });

            var type = (file.ContentType ?? string.Empty).ToUpperInvariant();
            if (type is not ("image/jpeg" or "image/jpg" or "image/png" or "image/webp"))
                return BadRequest(new { Message = "Parcel photo must be a JPEG, PNG, or WebP" });

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer).ConfigureAwait(false);
            // Get a zero-allocation ReadOnlyMemory<byte> pointing to the internal buffer
            ReadOnlyMemory<byte> memoryData = buffer.TryGetBuffer(out var segment)
                ? segment.AsMemory(0, (int)buffer.Length)
                : buffer.GetBuffer().AsMemory(0, (int)buffer.Length);

            var result = await _mediator.Send(new SaveParcelPhotoCommand(clientPhotoId, memoryData, string.Equals(type, "image/jpg", StringComparison.Ordinal) ? "image/jpeg" : type)).ConfigureAwait(false);
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Error });

            return Ok(result.Body);
        }

        [HttpPost("{id:guid}/damage")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Damage(Guid id, [FromBody] DamageIntakeRequest request)
        {
            var result = await _mediator.Send(new SaveDamageCommand(id, request ?? new DamageIntakeRequest())).ConfigureAwait(false);
            if (!result.Ok && string.Equals(result.Error, "Intake not found", StringComparison.Ordinal))
                return NotFound(new { Message = result.Error });
            if (!result.Ok)
                return BadRequest(new { Message = result.Error });

            return Ok(new { Message = "Damage photo saved" });
        }
    }
}
