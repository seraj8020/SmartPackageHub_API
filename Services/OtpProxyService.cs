using System.Net.Http.Json;
using System.Text.Json;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Services
{
    public class OtpProxyService : IOtpService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly string _baseUrl;

        public OtpProxyService(AppDbContext db, IHttpClientFactory http, IConfiguration config)
        {
            _db = db;
            _http = http;
            _config = config;
            _baseUrl = _config.GetValue<string>("OtpMicroservice:BaseUrl")?.TrimEnd('/') ?? "http://localhost:5005";
        }

        public async Task<string> GenerateOtpAsync(Guid residentId)
        {
            var resident = await _db.Residents.FindAsync(residentId);
            if (resident == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(resident.PhoneNumber)) return string.Empty;

            var client = _http.CreateClient();
            var url = $"{_baseUrl}/api/otp/generate";
            var payload = new { PhoneNumber = resident.PhoneNumber, RecipientId = resident.Id.ToString(), Purpose = "pickup" };
            var resp = await client.PostAsJsonAsync(url, payload);
            if (!resp.IsSuccessStatusCode) return string.Empty;
            var obj = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (obj.TryGetProperty("Otp", out var otpEl)) return otpEl.GetString() ?? string.Empty;
            return string.Empty;
        }

        public async Task<bool> ValidateOtpAsync(Guid residentId, string code)
        {
            var resident = await _db.Residents.FindAsync(residentId);
            if (resident == null) return false;
            if (string.IsNullOrWhiteSpace(resident.PhoneNumber)) return false;

            var client = _http.CreateClient();
            var url = $"{_baseUrl}/api/otp/verify";
            var payload = new { PhoneNumber = resident.PhoneNumber, Code = code, Purpose = "pickup" };
            var resp = await client.PostAsJsonAsync(url, payload);
            if (!resp.IsSuccessStatusCode) return false;
            var obj = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (obj.TryGetProperty("success", out var s)) return s.GetBoolean();
            return false;
        }
    }
}
