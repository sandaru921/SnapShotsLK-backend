using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapShotsLK.API.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        public int ProfileId { get; set; }

        [ForeignKey("ProfileId")]
        public ProfessionalProfile? ProfessionalProfile { get; set; }

        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerAvatar { get; set; } = string.Empty;
        public int Rating { get; set; } = 5;
        public string Comment { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public List<string> Images { get; set; } = new List<string>();
    }
}
