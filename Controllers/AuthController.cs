using Microsoft.AspNetCore.Mvc;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Services;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IOtpService _otp;
        private readonly ITokenService _tokens;

        public AuthController(AppDbContext db, IOtpService otp, ITokenService tokens)
        {
            _db = db;
            _otp = otp;
            _tokens = tokens;
        }

        public class LoginOtpRequest { public Guid ResidentId { get; set; } }
        public class VerifyOtpRequest { public Guid ResidentId { get; set; } public string Code { get; set; } }

        [HttpPost("login-otp")]
        public async Task<IActionResult> LoginOtp([FromBody] LoginOtpRequest req)
        {
            var resident = await _db.Residents.FindAsync(req.ResidentId);
            if (resident == null) return NotFound(new { message = "Resident not found" });
            var code = await _otp.GenerateOtpAsync(resident.Id);
            return Ok(new { resident.Id, Otp = code });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            var resident = await _db.Residents.FindAsync(req.ResidentId);
            if (resident == null) return NotFound(new { message = "Resident not found" });
            var ok = await _otp.ValidateOtpAsync(resident.Id, req.Code);
            if (!ok) return BadRequest(new { message = "Invalid or expired OTP" });
            var token = _tokens.CreateToken(resident.Id, resident.Name);
            return Ok(new { token, resident });
        }
    }
}
