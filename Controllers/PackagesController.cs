using Microsoft.AspNetCore.Mvc;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PackagesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public PackagesController(AppDbContext db) { _db = db; }

        public class CreatePackageRequest { public string TrackingNumber { get; set; } public string? Description { get; set; } public Guid? ResidentId { get; set; } }
        // POST: api/packages - create a package (assign to resident optionally)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePackageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TrackingNumber)) return BadRequest(new { Message = "TrackingNumber is required" });
            var pkg = new Package { TrackingNumber = req.TrackingNumber, Description = req.Description, ResidentId = req.ResidentId };
            _db.Packages.Add(pkg);
            await _db.SaveChangesAsync();
            return Ok(pkg);
        }
    }
}
