using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.DTOs;
using SnapShotsLK.API.Models;

namespace SnapShotsLK.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "superadmin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/admin/stats — platform-wide counts
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalClients       = await _context.Users.CountAsync(u => u.Role == "Client");
            var totalProfessionals = await _context.Users.CountAsync(u => u.Role == "admin");
            var totalPending       = await _context.Users.CountAsync(u => u.Role == "pending_admin");
            var totalRejected      = await _context.Users.CountAsync(u => u.Role == "rejected_admin");
            var totalBookings      = await _context.Bookings.CountAsync(b => b.Type == "booking");
            var pendingBookings    = await _context.Bookings.CountAsync(b => b.Type == "booking" && b.Status == "pending");
            var confirmedBookings  = await _context.Bookings.CountAsync(b => b.Type == "booking" && b.Status == "confirmed");

            return Ok(new
            {
                totalClients,
                totalProfessionals,
                totalPending,
                totalRejected,
                totalBookings,
                pendingBookings,
                confirmedBookings,
            });
        }

        // GET /api/admin/pending — all pending_admin users
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingAdmins()
        {
            var pending = await _context.Users
                .Where(u => u.Role == "pending_admin")
                .OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId, u.Name, u.Email, u.Phone, u.Location,
                    u.ServiceType, u.BusinessName, u.NicNumber,
                    u.NicDocumentUrl, u.BusinessCertUrl, u.PortfolioUrl,
                    u.Role, u.IsApproved
                })
                .ToListAsync();
            return Ok(pending);
        }

        // GET /api/admin/all — all approved professionals
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var admins = await _context.Users
                .Where(u => u.Role == "admin")
                .OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId, u.Name, u.Email, u.ServiceType,
                    u.BusinessName, u.Location, u.Phone, u.IsApproved
                })
                .ToListAsync();
            return Ok(admins);
        }

        // GET /api/admin/clients — all registered clients
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _context.Users
                .Where(u => u.Role == "Client")
                .OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId, u.Name, u.Email, u.Phone, u.Location, u.IsApproved
                })
                .ToListAsync();
            return Ok(clients);
        }

        // GET /api/admin/rejected — all rejected applications
        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedAdmins()
        {
            var rejected = await _context.Users
                .Where(u => u.Role == "rejected_admin")
                .OrderByDescending(u => u.UserId)
                .Select(u => new
                {
                    u.UserId, u.Name, u.Email, u.Phone, u.Location,
                    u.ServiceType, u.BusinessName, u.NicNumber, u.RejectionReason
                })
                .ToListAsync();
            return Ok(rejected);
        }

        // GET /api/admin/bookings — all platform bookings
        [HttpGet("bookings")]
        public async Task<IActionResult> GetAllBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Professional)
                .Where(b => b.Type == "booking")
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.BookingId, b.BookingDate, b.TimeSlot,
                    b.Status, b.PackageName, b.PackagePrice, b.CreatedAt,
                    client = new
                    {
                        id   = b.Client!.UserId,
                        name = b.Client.Name ?? b.Client.Email,
                        email = b.Client.Email,
                    },
                    professional = new
                    {
                        id          = b.Professional!.UserId,
                        name        = b.Professional.BusinessName ?? b.Professional.Name ?? b.Professional.Email,
                        serviceType = b.Professional.ServiceType,
                    }
                })
                .ToListAsync();
            return Ok(bookings);
        }

        // POST /api/admin/approve — approve or reject a pending_admin
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveOrRejectAdmin([FromBody] AdminApprovalDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return NotFound(new { message = "User not found." });
            if (user.Role != "pending_admin") return BadRequest(new { message = "This user is not in a pending state." });

            if (dto.Action.ToLower() == "approve")
            {
                user.Role = "admin";
                user.IsApproved = true;
                user.RejectionReason = null;

                var existingProfile = await _context.ProfessionalProfiles
                    .FirstOrDefaultAsync(p => p.UserId == user.UserId);
                if (existingProfile == null)
                {
                    _context.ProfessionalProfiles.Add(new ProfessionalProfile
                    {
                        UserId        = user.UserId,
                        Bio           = $"{user.BusinessName ?? user.Name ?? "Professional"} — {user.ServiceType} based in {user.Location ?? "Sri Lanka"}.",
                        Experience    = "",
                        About         = "",
                        ResponseTime  = "Within 24 hours",
                        Availability  = "Available",
                        Specialties   = new List<string>(),
                        PortfolioUrls = new List<string>(),
                        Languages     = new List<string> { "Sinhala", "English" },
                        Achievements  = new List<string>(),
                    });
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{user.Name ?? user.Email} has been approved." });
            }
            else if (dto.Action.ToLower() == "reject")
            {
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    return BadRequest(new { message = "A rejection reason is required." });

                user.Role = "rejected_admin";
                user.IsApproved = false;
                user.RejectionReason = dto.RejectionReason;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{user.Name ?? user.Email}'s application has been rejected." });
            }
            return BadRequest(new { message = "Invalid action. Use 'approve' or 'reject'." });
        }

        // POST /api/admin/re-review/{userId} — move rejected back to pending
        [HttpPost("re-review/{userId}")]
        public async Task<IActionResult> ReReview(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });
            if (user.Role != "rejected_admin") return BadRequest(new { message = "This user is not in a rejected state." });

            user.Role = "pending_admin";
            user.IsApproved = false;
            user.RejectionReason = null;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"{user.Name ?? user.Email} moved back to pending review." });
        }

        // DELETE /api/admin/{userId} — suspend a professional
        [HttpDelete("{userId}")]
        public async Task<IActionResult> SuspendProfessional(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });
            if (user.Role != "admin") return BadRequest(new { message = "This user is not an active professional." });

            user.Role = "Client";
            user.IsApproved = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"{user.Name ?? user.Email} has been suspended." });
        }
    }
}
