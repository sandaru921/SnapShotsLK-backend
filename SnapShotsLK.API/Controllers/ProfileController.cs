using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.DTOs;
using SnapShotsLK.API.Models;
using System.Security.Claims;

namespace SnapShotsLK.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(emailClaim)) return null;

            var user = _context.Users.FirstOrDefault(u => u.Email == emailClaim);
            return user?.UserId;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized("Invalid token or user not found.");

            var profile = await _context.ProfessionalProfiles
                .Include(p => p.Packages)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return Ok(new { exists = false });
            }

            return Ok(new { exists = true, profile });
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpsertProfile([FromBody] ProfileUpdateDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized("Invalid token or user not found.");

                var profile = await _context.ProfessionalProfiles
                    .Include(p => p.Packages)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    profile = new ProfessionalProfile { UserId = userId.Value };
                    _context.ProfessionalProfiles.Add(profile);
                }

                // Map DTO to Entity
                profile.Bio = dto.Bio ?? "";
                profile.AvatarUrl = dto.AvatarUrl ?? "";
                profile.CoverImageUrl = dto.CoverImageUrl ?? "";
                profile.Experience = dto.Experience ?? "";
                profile.About = dto.About ?? "";
                profile.ResponseTime = dto.ResponseTime ?? "";
                profile.Availability = dto.Availability ?? "";
                profile.InstagramUrl = dto.InstagramUrl ?? "";
                profile.FacebookUrl = dto.FacebookUrl ?? "";
                profile.WebsiteUrl = dto.WebsiteUrl ?? "";
                
                profile.Specialties = dto.Specialties ?? new List<string>();
                profile.PortfolioUrls = dto.PortfolioUrls ?? new List<string>();
                profile.Languages = dto.Languages ?? new List<string>();
                profile.Achievements = dto.Achievements ?? new List<string>();

                // Handle Packages safely without breaking EF tracking
                if (profile.Packages != null && profile.Packages.Any())
                {
                    _context.ServicePackages.RemoveRange(profile.Packages);
                }
                
                profile.Packages ??= new List<ServicePackage>();
                
                if (dto.Packages != null && dto.Packages.Any())
                {
                    foreach(var pkg in dto.Packages)
                    {
                        profile.Packages.Add(new ServicePackage
                        {
                            Name = pkg.Name ?? "",
                            Price = pkg.Price ?? "",
                            Duration = pkg.Duration ?? "",
                            IsPopular = pkg.IsPopular,
                            Features = pkg.Features ?? new List<string>()
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Profile updated safely", profile });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server Error: " + ex.Message, details = ex.InnerException?.Message });
            }
        }

        [HttpGet("public/category/{serviceType}")]
        public async Task<IActionResult> GetProfilesByCategory(string serviceType)
        {
            var users = await _context.Users
                .Include(u => u.ProfessionalProfile)
                .Where(u =>
                    u.IsApproved &&
                    u.ServiceType != null &&
                    u.ServiceType.ToLower() == serviceType.ToLower() &&
                    u.ProfessionalProfile != null)
                .OrderByDescending(u => u.ProfessionalProfile!.Rating)
                .Select(u => new
                {
                    id = u.UserId,
                    name = u.Name ?? u.Email,
                    businessName = u.BusinessName,
                    location = u.Location,
                    serviceType = u.ServiceType,
                    profile = new
                    {
                        bio = u.ProfessionalProfile!.Bio,
                        avatarUrl = u.ProfessionalProfile!.AvatarUrl,
                        coverImageUrl = u.ProfessionalProfile!.CoverImageUrl,
                        rating = u.ProfessionalProfile!.Rating,
                        reviewCount = u.ProfessionalProfile!.ReviewCount,
                        experience = u.ProfessionalProfile!.Experience,
                        specialties = u.ProfessionalProfile!.Specialties,
                    }
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("public/{userId}")]
        public async Task<IActionResult> GetPublicProfile(int userId)
        {
            var user = await _context.Users
                .Include(u => u.ProfessionalProfile)
                .ThenInclude(p => p.Packages)
                .Include(u => u.ProfessionalProfile)
                .ThenInclude(p => p.Reviews)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound("Professional user not found");

            // Build payload exactly mapping to ProfileView frontend expectations
            var responsePayload = new
            {
                id = user.UserId.ToString(),
                name = user.Name ?? user.BusinessName ?? user.Email,
                bio = user.ProfessionalProfile?.Bio ?? "",
                avatar = user.ProfessionalProfile?.AvatarUrl ?? "/images/placeholder-avatar.jpg",
                coverImage = user.ProfessionalProfile?.CoverImageUrl ?? "/images/placeholder-cover.jpg",
                rating = user.ProfessionalProfile?.Rating ?? 5.0,
                reviewCount = user.ProfessionalProfile?.ReviewCount ?? 0,
                location = user.Location ?? "",
                experience = user.ProfessionalProfile?.Experience ?? "",
                specialty = user.ProfessionalProfile?.Specialties ?? new List<string>(),
                about = user.ProfessionalProfile?.About ?? "",
                packages = user.ProfessionalProfile?.Packages?.Select(pkg => (object)new {
                    name = pkg.Name,
                    price = pkg.Price,
                    duration = pkg.Duration,
                    features = pkg.Features,
                    popular = pkg.IsPopular
                }).ToList() ?? new List<object>(),
                portfolio = user.ProfessionalProfile?.PortfolioUrls ?? new List<string>(),
                reviews = user.ProfessionalProfile?.Reviews?.Select(r => (object)new {
                    name = r.ReviewerName,
                    avatar = r.ReviewerAvatar,
                    rating = r.Rating,
                    date = r.Date.ToString("yyyy-MM-dd"),
                    comment = r.Comment,
                    images = r.Images
                }).ToList() ?? new List<object>(),
                achievements = user.ProfessionalProfile?.Achievements ?? new List<string>(),
                responseTime = user.ProfessionalProfile?.ResponseTime ?? "2 hours",
                languages = user.ProfessionalProfile?.Languages ?? new List<string>(),
                socialMedia = new {
                    instagram = user.ProfessionalProfile?.InstagramUrl,
                    facebook = user.ProfessionalProfile?.FacebookUrl,
                    website = user.ProfessionalProfile?.WebsiteUrl
                },
                contact = new {
                    email = user.Email,
                    phone = user.Phone ?? ""
                },
                availability = user.ProfessionalProfile?.Availability ?? "Available"
            };

            return Ok(responsePayload);
        }
    }
}
