using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SocietiesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public SocietiesController(AppDbContext db) { _db = db; }

        // GET: api/societies
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Societies.ToListAsync();
            return Ok(list);
        }

        // GET: api/societies/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var item = await _db.Societies.FindAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        public class CreateSocietyRequest { public string Name { get; set; } public string? Address { get; set; } public string? ContactEmail { get; set; } public string? Phone { get; set; } }

        // POST: api/societies
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSocietyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { Message = "Name is required" });
            var s = new Society { Name = req.Name, Address = req.Address, ContactEmail = req.ContactEmail, Phone = req.Phone };
            _db.Societies.Add(s);
            await _db.SaveChangesAsync();
            return Ok(s);
        }

        public class UpdateSocietyRequest { public string? Name { get; set; } public string? Address { get; set; } public string? ContactEmail { get; set; } public string? Phone { get; set; } }

        // PUT: api/societies/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSocietyRequest req)
        {
            var s = await _db.Societies.FindAsync(id);
            if (s == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(req.Name)) s.Name = req.Name;
            s.Address = req.Address;
            s.ContactEmail = req.ContactEmail;
            s.Phone = req.Phone;
            await _db.SaveChangesAsync();
            return Ok(s);
        }

        // DELETE: api/societies/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var s = await _db.Societies.FindAsync(id);
            if (s == null) return NotFound();
            _db.Societies.Remove(s);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
