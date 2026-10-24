using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartPackageHub.Common.Types;
using SmartPackageHub.HubManagement.Infrastructure;

namespace SmartPackageHub.HubManagement.Api.Controllers;

/// <summary>
/// Development resident sign-in endpoints. Replace the returned OTP with an SMS
/// provider before deploying this API outside a trusted development environment.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/auth/resident/otp")]
public sealed class ResidentAuthController : ControllerBase
{
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    private readonly HubDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _environment;

    public ResidentAuthController(HubDbContext db, IMemoryCache cache, IHostEnvironment environment)
    {
        _db = db;
        _cache = cache;
        _environment = environment;
    }

    [HttpPost("request")]
    [MapToApiVersion(ApiVersions.V2)]
    public async Task<ActionResult> Request([FromBody] RequestResidentLoginOtpRequest request)
    {
        var phoneNumber = NormalizePhone(request.PhoneNumber);
        if (phoneNumber.Length != 10)
            return BadRequest(new { Message = "Enter a valid 10-digit mobile number." });

        var resident = await _db.Residents.FirstOrDefaultAsync(item => item.PhoneNumber == phoneNumber && item.IsActive);
        if (resident is null)
            return NotFound(new { Message = "This mobile number is not registered. Please register first." });

        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        _cache.Set(CacheKey(resident.Id), otp, OtpLifetime);

        // The value is included only for the existing local-development UI.
        // A production integration should send it through an SMS provider instead.
        return Ok(new
        {
            ResidentId = resident.Id,
            Otp = _environment.IsDevelopment() ? otp : null,
            Message = "Sign-in code generated."
        });
    }

    [HttpPost("verify")]
    [MapToApiVersion(ApiVersions.V2)]
    public async Task<ActionResult> Verify([FromBody] VerifyResidentLoginOtpRequest request)
    {
        var phoneNumber = NormalizePhone(request.PhoneNumber);
        var resident = await _db.Residents.FirstOrDefaultAsync(item => item.PhoneNumber == phoneNumber && item.IsActive);
        if (resident is null)
            return NotFound(new { Message = "This mobile number is not registered. Please register first." });

        if (!_cache.TryGetValue<string>(CacheKey(resident.Id), out var expectedOtp) ||
            !CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(expectedOtp), System.Text.Encoding.UTF8.GetBytes(request.Otp ?? string.Empty)))
            return BadRequest(new { Message = "That code is not correct or has expired. Please request a new one." });

        _cache.Remove(CacheKey(resident.Id));
        return Ok(new { Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), ResidentId = resident.Id });
    }

    private static string NormalizePhone(string? value) => new(value?.Where(char.IsDigit).Take(10).ToArray() ?? []);
    private static string CacheKey(Guid residentId) => $"resident-login-otp:{residentId}";
}

public sealed class RequestResidentLoginOtpRequest
{
    public string? PhoneNumber { get; set; }
}

public sealed class VerifyResidentLoginOtpRequest
{
    public string? PhoneNumber { get; set; }
    public string? Otp { get; set; }
}
