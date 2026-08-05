using Microsoft.AspNetCore.Mvc;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public MembersController(AppDbContext db) { _db = db; }

        public class CreateMemberRequest { public string Name { get; set; } public string? Email { get; set; } public string? PhoneNumber { get; set; } }

        // POST: api/members - create new member (resident signup)
        [HttpPost]
        public async Task<IActionResult> CreateMember([FromBody] CreateMemberRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { Message = "Name is required" });
            var resident = new Resident { Name = req.Name, Email = req.Email, PhoneNumber = req.PhoneNumber, IsMember = true };
            _db.Residents.Add(resident);
            await _db.SaveChangesAsync();
            return Ok(resident);
        }
    }
}
