using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPackageHub_API.Data;
using SmartPackageHub_API.Models;

namespace SmartPackageHub_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) { _db = db; }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _db.Users.ToListAsync();
            return Ok(list.Select(u => new { u.Id, u.Username, u.Email, u.FullName, u.IsAdmin }));
        }

        // GET: api/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return Ok(new { user.Id, user.Username, user.Email, user.FullName, user.IsAdmin });
        }

        public class CreateUserRequest { public string Username { get; set; } public string Email { get; set; } public string? FullName { get; set; } public string? Password { get; set; } public bool IsAdmin { get; set; } }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest(new { Message = "Username is required" });
            if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest(new { Message = "Email is required" });

            var exists = await _db.Users.AnyAsync(u => u.Username == req.Username || u.Email == req.Email);
            if (exists) return Conflict(new { Message = "A user with the same username or email already exists" });

            var user = new User { Username = req.Username, Email = req.Email, FullName = req.FullName, IsAdmin = req.IsAdmin, Password = req.Password };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { user.Id, user.Username, user.Email, user.FullName, user.IsAdmin });
        }

        public class UpdateUserRequest { public string? Email { get; set; } public string? FullName { get; set; } public bool? IsAdmin { get; set; } }

        // PUT: api/users/{id}
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (!string.IsNullOrWhiteSpace(req.Email)) user.Email = req.Email;
            user.FullName = req.FullName ?? user.FullName;
            if (req.IsAdmin.HasValue) user.IsAdmin = req.IsAdmin.Value;
            await _db.SaveChangesAsync();
            return Ok(new { user.Id, user.Username, user.Email, user.FullName, user.IsAdmin });
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
