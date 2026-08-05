using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;
using SmartPackageHub_API.Services;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResidentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IOtpService _otpService;

        public ResidentsController(AppDbContext db, IOtpService otpService)
        {
            _db = db;
            _otpService = otpService;
        }

        // GET: api/residents/{id}/packages - list packages currently at hub for resident
        [HttpGet("{id:guid}/packages")]
        public async Task<IActionResult> GetPackages(Guid id)
        {
            var resident = await _db.Residents.Include(r => r.Packages).FirstOrDefaultAsync(r => r.Id == id);
            if (resident == null) return NotFound();
            var packages = resident.Packages.Where(p => !p.IsPickedUp).Select(p => new { p.Id, p.TrackingNumber, p.Description, p.ReceivedAt, p.Courier });
            return Ok(packages);
        }

        // POST: api/residents/{id}/otp - request OTP
        [HttpPost("{id:guid}/otp")]
        public async Task<IActionResult> RequestOtp(Guid id)
        {
            var resident = await _db.Residents.FindAsync(id);
            if (resident == null) return NotFound();

            var code = await _otpService.GenerateOtpAsync(id);

            // In production, send via SMS/Email; here return the code for testing.
            
            return Ok(new { resident.Id, Otp = code, Message = "OTP generated (returning in response for demo)." });
        }

        public class VerifyOtpRequest { public string Code { get; set; } }

        // POST: api/residents/{id}/verify-otp - verify OTP and pickup packages
        [HttpPost("{id:guid}/verify-otp")]
        public async Task<IActionResult> VerifyOtp(Guid id, [FromBody] VerifyOtpRequest request)
        {
            var resident = await _db.Residents.Include(r => r.Packages).FirstOrDefaultAsync(r => r.Id == id);
            if (resident == null) return NotFound();

            var ok = await _otpService.ValidateOtpAsync(id, request.Code);
            if (!ok) return BadRequest(new { Message = "Invalid or expired OTP." });

            // Mark packages as picked up and create DeliveryHistory entries
            foreach (var pkg in resident.Packages.Where(p => !p.IsPickedUp))
            {
                pkg.IsPickedUp = true;
                _db.DeliveryHistories.Add(new DeliveryHistory { PackageId = pkg.Id, DeliveredAt = DateTime.UtcNow, Notes = "Picked up via OTP" });
            }
            await _db.SaveChangesAsync();
            return Ok(new { Message = "OTP verified. Packages marked picked up." });
        }

        // GET: api/residents/{id}/history - delivery history for resident's packages
        
        [HttpGet("{id:guid}/history")]
        public async Task<IActionResult> GetHistory(Guid id)
        {
            var resident = await _db.Residents.Include(r => r.Packages).FirstOrDefaultAsync(r => r.Id == id);
            if (resident == null) return NotFound();

            var packageIds = resident.Packages.Select(p => p.Id).ToList();
            var history = await _db.DeliveryHistories.Where(h => packageIds.Contains(h.PackageId)).OrderByDescending(h => h.DeliveredAt).ToListAsync();
            return Ok(history);
        }
    }
}
