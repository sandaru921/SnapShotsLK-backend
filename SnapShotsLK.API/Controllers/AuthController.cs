using Microsoft.AspNetCore.Mvc;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.DTOs;
using SnapShotsLK.API.Models;
using BCrypt.Net;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace SnapShotsLK.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ─────────────────────────────────────────────
        //  Register: regular client user
        // ─────────────────────────────────────────────
        [HttpPost("register")]
        public IActionResult Register(UserRegisterDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest("User already exists.");
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "Client",
                IsApproved = true   // Clients are approved immediately
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok("User Registered Successfully!");
        }

        // ─────────────────────────────────────────────
        //  Register: professional / admin
        //  Submits proof docs → status is pending_admin
        //  SuperAdmin must approve before they can login
        // ─────────────────────────────────────────────
        [HttpPost("register-professional")]
        public IActionResult RegisterProfessional(RegisterProfessionalDto request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "User already exists with this email." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Location = request.Location,
                PasswordHash = passwordHash,
                Role = "pending_admin",     // Always starts as pending
                IsApproved = false,         // Requires SuperAdmin approval
                ServiceType = request.ServiceType,
                BusinessName = request.BusinessName,
                NicNumber = request.NicNumber,
                NicDocumentUrl = request.NicDocumentUrl,
                BusinessCertUrl = request.BusinessCertUrl,
                PortfolioUrl = request.PortfolioUrl
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            // Do NOT return a JWT — they cannot login until approved
            return Ok(new
            {
                message = "Registration submitted. Your account is pending SuperAdmin approval.",
                status = "pending_admin"
            });
        }

        // ─────────────────────────────────────────────
        //  Login (all roles share this endpoint)
        // ─────────────────────────────────────────────
        [HttpPost("login")]
        public IActionResult Login(UserLoginDto request)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }

            // Block pending admins from logging in
            if (user.Role == "pending_admin")
            {
                return StatusCode(403, new
                {
                    message = "Your account is pending SuperAdmin approval. You will be notified once approved.",
                    status = "pending_admin"
                });
            }

            // Block rejected admins
            if (user.Role == "rejected_admin")
            {
                return StatusCode(403, new
                {
                    message = $"Your account application was rejected. Reason: {user.RejectionReason ?? "No reason provided."}",
                    status = "rejected_admin"
                });
            }

            string token = CreateToken(user);

            return Ok(new
            {
                token = token,
                user = new
                {
                    id = user.UserId.ToString(),
                    name = user.Name ?? user.Email,
                    email = user.Email,
                    role = user.Role,
                    phone = user.Phone,
                    location = user.Location
                }
            });
        }

        // ─────────────────────────────────────────────
        //  JWT helper
        // ─────────────────────────────────────────────
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("JwtSettings:Key").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
    }
}