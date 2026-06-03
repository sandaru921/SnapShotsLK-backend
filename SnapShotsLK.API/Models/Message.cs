using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapShotsLK.API.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int SenderId { get; set; }
        
        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [Required]
        public int ReceiverId { get; set; }
        
        [ForeignKey(nameof(ReceiverId))]
        public User? Receiver { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
