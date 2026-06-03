using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapShotsLK.API.Data;
using System.Security.Claims;

namespace SnapShotsLK.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        private SnapShotsLK.API.Models.User? GetCurrentUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email)) return null;
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        // ── GET /api/user/me
        // Returns the logged-in user's own profile data
        [HttpGet("me")]
        public IActionResult GetMe()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            return Ok(new
            {
                id = user.UserId,
                name = user.Name,
                email = user.Email,
                phone = user.Phone,
                location = user.Location,
                role = user.Role,
                serviceType = user.ServiceType,
                businessName = user.BusinessName,
            });
        }

        // ── PATCH /api/user/me
        // Updates name, phone, location for the logged-in user
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            if (!string.IsNullOrWhiteSpace(dto.Name))    user.Name = dto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone))   user.Phone = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Location)) user.Location = dto.Location.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Profile updated successfully.",
                name = user.Name,
                phone = user.Phone,
                location = user.Location,
            });
        }
    }

    public class UpdateUserDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
    }
}
