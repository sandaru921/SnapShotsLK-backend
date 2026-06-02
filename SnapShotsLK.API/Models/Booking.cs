using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapShotsLK.API.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        // Who is booking
        [Required]
        public int ClientUserId { get; set; }
        [ForeignKey("ClientUserId")]
        public User? Client { get; set; }

        // Who is being booked
        [Required]
        public int ProfessionalUserId { get; set; }
        [ForeignKey("ProfessionalUserId")]
        public User? Professional { get; set; }

        // What is being booked
        public string? PackageName { get; set; }
        public string? PackagePrice { get; set; }

        // When
        [Required]
        public string BookingDate { get; set; } = string.Empty;  // "YYYY-MM-DD"
        [Required]
        public string TimeSlot { get; set; } = string.Empty;     // "HH:MM"

        // Status: pending | confirmed | cancelled | blocked
        public string Status { get; set; } = "pending";

        // "booking" = client-submitted | "blocked" = admin-blocked their own slot
        public string Type { get; set; } = "booking";

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
