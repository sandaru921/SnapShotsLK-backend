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
                Role = "Client"
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok("User Registered Successfully!");
        }

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
                Role = string.IsNullOrEmpty(request.Role) ? "admin" : request.Role,
                ServiceType = request.ServiceType,
                BusinessName = request.BusinessName
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            string token = CreateToken(newUser);

            return Ok(new
            {
                token = token,
                user = new
                {
                    id = newUser.UserId,
                    name = newUser.Name,
                    email = newUser.Email,
                    role = newUser.Role,
                    phone = newUser.Phone,
                    location = newUser.Location
                }
            });
        }

        
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

            
            string token = CreateToken(user);

            return Ok(token);
        }

        
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