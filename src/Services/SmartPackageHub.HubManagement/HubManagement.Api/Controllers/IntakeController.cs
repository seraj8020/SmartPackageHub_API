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
        public async Task<ActionResult> Shelf(
            [FromQuery] Guid? societyId,
            [FromQuery] string? towerOrWing = null,
            [FromQuery] bool? isColdStorage = null)
        {
            var shelf = await _mediator
                .Send(new SuggestShelfQuery(societyId, towerOrWing, isColdStorage))
                .ConfigureAwait(false);
            if (shelf == null)
                return NotFound(new { Message = "No shelf is available" });

            return Ok(shelf);
        }

        /// <summary>
        /// Distinct active carrier names for staff intake / scan courier dropdown.
        /// </summary>
        [HttpGet("carriers")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<ActionResult> Carriers([FromQuery] Guid? societyId, CancellationToken cancellationToken)
        {
            var carriers = await _mediator
                .Send(new GetIntakeCarriersQuery(societyId), cancellationToken)
                .ConfigureAwait(false);
            return Ok(carriers);
        }

        [HttpPost("confirm")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(ConfirmIntakeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Confirm([FromBody] ConfirmIntakeRequest request)
        {
            IntakeStatusResult<ConfirmIntakeResponse> result;
            try
            {
                result = await _mediator.Send(new ConfirmIntakeCommand(request ?? new ConfirmIntakeRequest())).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Map unexpected domain failures to a stable JSON 500 for mobile clients.
            catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = string.IsNullOrWhiteSpace(ex.Message) ? "Intake failed unexpectedly" : ex.Message,
                });
            }

            if (result.StatusCode == 400)
                return BadRequest(new { Message = result.Error });
            if (result.StatusCode == 404)
                return NotFound(new { Message = result.Error });
            if (result.StatusCode == 409)
                return Conflict(new { Message = result.Error });
            if (result.StatusCode >= 500)
                return StatusCode(result.StatusCode, new { Message = result.Error ?? "Intake failed" });

            return Ok(result.Body);
        }

        /// <summary>
        /// Confirm multiple scanned packages in one call (offline mobile queue flush).
        /// Each item uses the same rules as POST confirm, including clientRequestId idempotency.
        /// Partial success returns HTTP 200 with per-item status codes.
        /// </summary>
        [HttpPost("batch-intake")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(BatchIntakeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> BatchIntake([FromBody] BatchIntakeRequest request)
        {
            var result = await _mediator
                .Send(new BatchIntakeCommand(request ?? new BatchIntakeRequest()))
                .ConfigureAwait(false);

            if (result.StatusCode == 400)
                return BadRequest(new { Message = result.Error });

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

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer).ConfigureAwait(false);
            // Get a zero-allocation ReadOnlyMemory<byte> pointing to the internal buffer
            ReadOnlyMemory<byte> memoryData = buffer.TryGetBuffer(out var segment)
                ? segment.AsMemory(0, (int)buffer.Length)
                : buffer.GetBuffer().AsMemory(0, (int)buffer.Length);

            var type = ResolveParcelPhotoContentType(file, memoryData.Span);
            if (type is null)
                return BadRequest(new { Message = "Parcel photo must be a JPEG, PNG, or WebP" });

            var result = await _mediator.Send(new SaveParcelPhotoCommand(clientPhotoId, memoryData, type)).ConfigureAwait(false);
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Error });

            return Ok(result.Body);
        }

        /// <summary>
        /// Preferred mobile path — same base64 JSON pattern as damage photos (no multipart Content-Type issues).
        /// </summary>
        [HttpPost("photos/base64")]
        [MapToApiVersion(ApiVersions.V2)]
        [RequestSizeLimit(8_000_000)]
        [ProducesResponseType(typeof(PhotoUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> PhotoBase64([FromBody] ParcelPhotoBase64Request request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.ClientPhotoId) || string.IsNullOrWhiteSpace(request.ImageBase64))
                return BadRequest(new { Message = "A photo id and image are required" });

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(StripDataUrl(request.ImageBase64));
            }
            catch (FormatException)
            {
                return BadRequest(new { Message = "Parcel photo was not valid base64" });
            }

            if (bytes.Length == 0)
                return BadRequest(new { Message = "A photo id and image are required" });
            if (bytes.Length > 5_000_000)
                return BadRequest(new { Message = "Parcel photo is too large" });

            var type = ResolveParcelPhotoContentType(
                request.ContentType,
                fileName: null,
                bytes);
            if (type is null)
                return BadRequest(new { Message = "Parcel photo must be a JPEG, PNG, or WebP" });

            var result = await _mediator.Send(
                    new SaveParcelPhotoCommand(request.ClientPhotoId, bytes, type))
                .ConfigureAwait(false);
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Error });

            return Ok(result.Body);
        }

        /// <summary>
        /// Re-link an already-uploaded arrival photo without re-sending image bytes (cheap confirm follow-up).
        /// </summary>
        [HttpPost("photos/link")]
        [MapToApiVersion(ApiVersions.V2)]
        [ProducesResponseType(typeof(PhotoUploadResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> LinkPhoto([FromBody] LinkParcelPhotoRequest request)
        {
            var result = await _mediator.Send(new LinkParcelPhotoCommand(request ?? new LinkParcelPhotoRequest()))
                .ConfigureAwait(false);
            if (!result.Succeeded)
                return BadRequest(new { Message = result.Error });

            return Ok(result.Body);
        }

        [HttpPost("{id:guid}/damage")]
        [MapToApiVersion(ApiVersions.V2)]
        // Base64 JSON is ~4/3 of JPEG size; allow up to ~5MB decoded image.
        [RequestSizeLimit(8_000_000)]
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

        private static string? ResolveParcelPhotoContentType(IFormFile file, ReadOnlySpan<byte> bytes) =>
            ResolveParcelPhotoContentType(file.ContentType, file.FileName, bytes);

        /// <summary>
        /// Mobile multipart often sends Content-Type with parameters (e.g. image/jpeg; charset=utf-8)
        /// or octet-stream. Prefer media type / extension / magic bytes over a strict exact match.
        /// </summary>
        private static string? ResolveParcelPhotoContentType(
            string? contentType,
            string? fileName,
            ReadOnlySpan<byte> bytes)
        {
            var mediaType = (contentType ?? string.Empty)
                .Split(';', 2)[0]
                .Trim()
                .ToUpperInvariant();

            if (mediaType is "IMAGE/JPEG" or "IMAGE/JPG")
                return "image/jpeg";
            if (mediaType is "IMAGE/PNG")
                return "image/png";
            if (mediaType is "IMAGE/WEBP")
                return "image/webp";

            var name = fileName ?? string.Empty;
            if (name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                return "image/jpeg";
            if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return "image/png";
            if (name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                return "image/webp";

            // Sniff camera JPEG / PNG / WebP when headers are wrong or empty.
            if (LooksLikeJpeg(bytes))
                return "image/jpeg";
            if (LooksLikePng(bytes))
                return "image/png";
            if (LooksLikeWebp(bytes))
                return "image/webp";

            if (string.IsNullOrWhiteSpace(mediaType) ||
                mediaType is "APPLICATION/OCTET-STREAM" or "BINARY/OCTET-STREAM" or "APPLICATION/X-WWW-FORM-URLENCODED")
                return "image/jpeg";

            return null;
        }

        private static bool LooksLikeJpeg(ReadOnlySpan<byte> bytes) =>
            bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

        private static bool LooksLikePng(ReadOnlySpan<byte> bytes) =>
            bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;

        private static bool LooksLikeWebp(ReadOnlySpan<byte> bytes) =>
            bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';

        private static string StripDataUrl(string value)
        {
            var trimmed = value.Trim();
            const string marker = "base64,";
            var index = trimmed.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return index >= 0 ? trimmed[(index + marker.Length)..] : trimmed;
        }
    }
}
