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

        // 1. REGISTER (ලියාපදිංචි වීම)
        [HttpPost("register")]
        public IActionResult Register(UserRegisterDto request)
        {
            // Email එක කලින් තියෙනවද බලනවා
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest("User already exists.");
            }

            // Password එක Hash කරනවා (ආරක්ෂිත කරනවා)
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = "Client"
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok("User Registered Successfully!");
        }

        // 2. LOGIN (ඇතුල් වීම)
        [HttpPost("login")]
        public IActionResult Login(UserLoginDto request)
        {
            // User ව Database එකෙන් හොයනවා
            var user = _context.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Password එක ගැලපෙනවද බලනවා
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return BadRequest("Wrong password.");
            }

            // හරි නම් Token එකක් හදනවා
            string token = CreateToken(user);

            return Ok(token);
        }

        // මේකෙන් තමයි Token එක හදන්නේ (Private Method)
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
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