using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapShotsLK.API.Models
{
    public class ProfessionalProfile
    {
        [Key]
        public int ProfileId { get; set; }

        [Required]
        public int UserId { get; set; }
        
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Bio { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string ResponseTime { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;

        // Social Media
        public string InstagramUrl { get; set; } = string.Empty;
        public string FacebookUrl { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;

        // PostgreSQL native string arrays mapping to List<string>
        public List<string> Specialties { get; set; } = new List<string>();
        public List<string> PortfolioUrls { get; set; } = new List<string>();
        public List<string> Languages { get; set; } = new List<string>();
        public List<string> Achievements { get; set; } = new List<string>();

        // Navigation Properties
        public ICollection<ServicePackage> Packages { get; set; } = new List<ServicePackage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        
        // Stats
        public double Rating { get; set; } = 5.0;
        public int ReviewCount { get; set; } = 0;
    }
}
