using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using System.Security.Claims;

namespace SnapShotsLK.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessageController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdStr, out var id) ? id : 0;
        }

        // GET /api/message/history/{otherUserId}
        [HttpGet("history/{otherUserId}")]
        public async Task<IActionResult> GetChatHistory(int otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();
            
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(messages);
        }

        // GET /api/message/collaborators
        [HttpGet("collaborators")]
        public async Task<IActionResult> GetCollaborators()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            // Get all other professionals
            var professionals = await _context.Users
                .Include(u => u.ProfessionalProfile)
                .Where(u => u.UserId != currentUserId && (u.Role == "admin" || u.Role == "superadmin"))
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.ServiceType,
                    ProfilePicture = u.ProfessionalProfile != null ? u.ProfessionalProfile.AvatarUrl : null
                })
                .ToListAsync();

            // Find the latest message for each professional to show in the list
            var result = new List<object>();

            foreach (var prof in professionals)
            {
                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == prof.UserId) ||
                                (m.SenderId == prof.UserId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.SenderId == prof.UserId && m.ReceiverId == currentUserId && !m.IsRead);

                result.Add(new
                {
                    Prof = prof,
                    LastMessage = lastMessage != null ? new
                    {
                        lastMessage.Content,
                        lastMessage.SentAt,
                        IsMine = lastMessage.SenderId == currentUserId
                    } : null,
                    UnreadCount = unreadCount
                });
            }

            return Ok(result.OrderByDescending(r => r.GetType().GetProperty("LastMessage")?.GetValue(r, null) != null ? 1 : 0));
        }

        // GET /api/message/clients
        // Returns all unique users (clients/regular users) who have messaged the current admin
        [HttpGet("clients")]
        public async Task<IActionResult> GetClients()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            // Find all unique user IDs who have exchanged messages with current admin
            var senderIds = await _context.Messages
                .Where(m => m.ReceiverId == currentUserId)
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var receiverIds = await _context.Messages
                .Where(m => m.SenderId == currentUserId)
                .Select(m => m.ReceiverId)
                .Distinct()
                .ToListAsync();

            var allUserIds = senderIds.Union(receiverIds)
                .Where(id => id != currentUserId)
                .Distinct()
                .ToList();

            // Load user info — only non-admin users (actual clients)
            var users = await _context.Users
                .Include(u => u.ProfessionalProfile)
                .Where(u => allUserIds.Contains(u.UserId) && u.Role != "admin" && u.Role != "superadmin")
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.ServiceType,
                    ProfilePicture = u.ProfessionalProfile != null ? u.ProfessionalProfile.AvatarUrl : null
                })
                .ToListAsync();

            var result = new List<object>();

            foreach (var client in users)
            {
                var lastMessage = await _context.Messages
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == client.UserId) ||
                                (m.SenderId == client.UserId && m.ReceiverId == currentUserId))
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new { m.Content, m.SentAt, IsMine = m.SenderId == currentUserId })
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.SenderId == client.UserId && m.ReceiverId == currentUserId && !m.IsRead);

                result.Add(new
                {
                    Prof = client,
                    LastMessage = lastMessage,
                    UnreadCount = unreadCount
                });
            }

            // Sort: unread first, then by last message time
            var sorted = result
                .OrderByDescending(r =>
                {
                    var uc = (int)r.GetType().GetProperty("UnreadCount")!.GetValue(r)!;
                    return uc;
                })
                .ThenByDescending(r =>
                {
                    var lm = r.GetType().GetProperty("LastMessage")?.GetValue(r);
                    if (lm == null) return DateTime.MinValue;
                    var prop = lm.GetType().GetProperty("SentAt");
                    return prop != null ? (DateTime)prop.GetValue(lm)! : DateTime.MinValue;
                });

            return Ok(sorted);
        }
    }
}
