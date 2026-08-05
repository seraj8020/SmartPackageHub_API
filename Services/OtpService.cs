using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Services
{
    public class OtpService : IOtpService
    {
        private readonly AppDbContext _db;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
        private readonly Random _rnd = new();

        public OtpService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GenerateOtpAsync(Guid residentId)
        {
            // remove old otps
            var now = DateTime.UtcNow;
            var code = _rnd.Next(100000, 999999).ToString();
            var otp = new OtpCode
            {
                ResidentId = residentId,
                Code = code,
                ExpiresAt = now.Add(_ttl),
                IsUsed = false
            };
            _db.OtpCodes.Add(otp);
            await _db.SaveChangesAsync();

            // For a real system: send via SMS/Email. For this demo return the OTP so caller can see it.
            return code;
        }

        public async Task<bool> ValidateOtpAsync(Guid residentId, string code)
        {
            var now = DateTime.UtcNow;
            var otp = await _db.OtpCodes
                .Where(o => o.ResidentId == residentId && !o.IsUsed && o.ExpiresAt >= now && o.Code == code)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if (otp == null) return false;
            otp.IsUsed = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
