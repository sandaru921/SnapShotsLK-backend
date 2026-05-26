using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.DTOs;

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

        // ─────────────────────────────────────────────
        //  GET /api/admin/pending
        //  Returns all users with role = pending_admin
        // ─────────────────────────────────────────────
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingAdmins()
        {
            var pending = await _context.Users
                .Where(u => u.Role == "pending_admin")
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Phone,
                    u.Location,
                    u.ServiceType,
                    u.BusinessName,
                    u.NicNumber,
                    u.NicDocumentUrl,
                    u.BusinessCertUrl,
                    u.PortfolioUrl,
                    u.Role,
                    u.IsApproved
                })
                .ToListAsync();

            return Ok(pending);
        }

        // ─────────────────────────────────────────────
        //  GET /api/admin/all
        //  Returns all active admins
        // ─────────────────────────────────────────────
        [HttpGet("all")]
        public async Task<IActionResult> GetAllAdmins()
        {
            var admins = await _context.Users
                .Where(u => u.Role == "admin")
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.ServiceType,
                    u.BusinessName,
                    u.Location,
                    u.IsApproved
                })
                .ToListAsync();

            return Ok(admins);
        }

        // ─────────────────────────────────────────────
        //  POST /api/admin/approve
        //  Approve or reject a pending admin
        // ─────────────────────────────────────────────
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveOrRejectAdmin([FromBody] AdminApprovalDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Role != "pending_admin")
                return BadRequest(new { message = "This user is not in a pending state." });

            if (dto.Action.ToLower() == "approve")
            {
                user.Role = "admin";
                user.IsApproved = true;
                user.RejectionReason = null;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{user.Name ?? user.Email} has been approved as an admin." });
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
            else
            {
                return BadRequest(new { message = "Invalid action. Use 'approve' or 'reject'." });
            }
        }

        // ─────────────────────────────────────────────
        //  DELETE /api/admin/{userId}
        //  Remove an admin account
        // ─────────────────────────────────────────────
        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveAdmin(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Role != "admin")
                return BadRequest(new { message = "This user is not an admin." });

            user.Role = "Client";
            user.IsApproved = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{user.Name ?? user.Email} has been demoted." });
        }
    }
}
