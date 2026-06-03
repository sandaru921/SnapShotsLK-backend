using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.Models;
using System.Security.Claims;

namespace SnapShotsLK.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int receiverId, string content)
        {
            var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdClaim) || !int.TryParse(senderIdClaim, out int senderId))
            {
                throw new HubException("Unauthorized: User ID not found.");
            }

            if (string.IsNullOrWhiteSpace(content)) return;

            // Save to database
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast to the receiver
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", new
            {
                messageId = message.MessageId,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                sentAt = message.SentAt,
                isRead = message.IsRead
            });
            
            // Also broadcast back to the sender so their UI updates automatically if they have multiple tabs open
            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                messageId = message.MessageId,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                sentAt = message.SentAt,
                isRead = message.IsRead
            });
        }
    }
}
