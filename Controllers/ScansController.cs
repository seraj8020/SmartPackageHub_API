using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScansController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ScansController(AppDbContext db) { _db = db; }

        public class ScanReceiveRequest { public string Vendor { get; set; } public string RawCode { get; set; } public string? Description { get; set; } public Guid? ResidentId { get; set; } }
        public class ScanHandoverRequest { public string Code { get; set; } public string? Notes { get; set; } }

        // POST: api/scans/receive
        // Used by staff when scanning an incoming parcel (Amazon / Flipkart)
        [HttpPost("receive")]
        public async Task<IActionResult> ScanAndReceive([FromBody] ScanReceiveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Vendor) || string.IsNullOrWhiteSpace(req.RawCode)) return BadRequest(new { Message = "Vendor and RawCode are required" });

            // Try to parse tracking number from raw scan depending on vendor
            var tracking = ParseTracking(req.Vendor, req.RawCode);

            // Generate a package number (simple incremental based on existing count)
            var nextNumber = (await _db.Packages.CountAsync()) + 1;
            var pkgNumber = $"PKG-{nextNumber:D5}";

            var pkg = new Package
            {
                PkgNumber = pkgNumber,
                TrackingNumber = tracking ?? req.RawCode,
                Description = req.Description,
                ReceivedAt = DateTime.UtcNow,
                Courier = req.Vendor,
                ResidentId = req.ResidentId
            };

            _db.Packages.Add(pkg);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Package received", Package = new { pkg.Id, pkg.PkgNumber, pkg.TrackingNumber, pkg.ReceivedAt, pkg.Courier, pkg.ResidentId } });
        }

        // POST: api/scans/handover
        // Used by staff when handing a package over to recipient (scan at handover)
        [HttpPost("handover")]
        public async Task<IActionResult> ScanAndHandover([FromBody] ScanHandoverRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Code)) return BadRequest(new { Message = "Code is required" });

            // Find by tracking number or pkg number
            var pkg = await _db.Packages.FirstOrDefaultAsync(p => p.TrackingNumber == req.Code || p.PkgNumber == req.Code);
            if (pkg == null) return NotFound(new { Message = "Package not found" });

            if (pkg.IsPickedUp) return BadRequest(new { Message = "Package already handed over" });

            pkg.IsPickedUp = true;
            _db.DeliveryHistories.Add(new DeliveryHistory { PackageId = pkg.Id, PkgNumber = pkg.PkgNumber, DeliveredAt = DateTime.UtcNow, Notes = req.Notes ?? "Handed over via scan", Courier = pkg.Courier, Category = pkg.Category, MethodOfDelivery = "Handover" });
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Package handed over", Package = new { pkg.Id, pkg.PkgNumber, pkg.TrackingNumber } });
        }

        // GET: api/scans/parse?vendor=Amazon&code=...  -> returns parsed tracking for debugging
        [HttpGet("parse")]
        public IActionResult Parse([FromQuery] string vendor, [FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(vendor) || string.IsNullOrWhiteSpace(code)) return BadRequest();
            var tracking = ParseTracking(vendor, code) ?? code;
            return Ok(new { Vendor = vendor, Raw = code, Tracking = tracking });
        }

        // Very small heuristics to try extract a tracking number from vendor-specific raw scans.
        // These are intentionally simple; UI can send structured values if available.
        private string? ParseTracking(string vendor, string raw)
        {
            vendor = vendor?.Trim().ToLowerInvariant() ?? string.Empty;
            raw = raw?.Trim() ?? string.Empty;

            // If raw already looks like a typical tracking (alphanumeric, length 6+), return it
            if (raw.Length >= 6 && raw.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')) return raw;

            // Vendor-specific heuristics
            if (vendor.Contains("amazon"))
            {
                // Amazon sometimes prefixes with "AMZ" or contains order id; try to extract alnum sequences
                var tokens = System.Text.RegularExpressions.Regex.Matches(raw, "[A-Z0-9]{6,}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (tokens.Count > 0) return tokens[0].Value;
            }

            if (vendor.Contains("flipkart"))
            {
                // Flipkart tracking often numeric-heavy; extract first numeric sequence of length >=6
                var tokens = System.Text.RegularExpressions.Regex.Matches(raw, "[0-9]{6,}");
                if (tokens.Count > 0) return tokens[0].Value;
            }

            // Fallback: return raw if not empty
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }
    }
}
