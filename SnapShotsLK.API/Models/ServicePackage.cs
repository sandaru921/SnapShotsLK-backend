using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SnapShotsLK.API.Models
{
    public class ServicePackage
    {
        [Key]
        public int PackageId { get; set; }

        [Required]
        public int ProfileId { get; set; }

        [ForeignKey("ProfileId")]
        public ProfessionalProfile? ProfessionalProfile { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public bool IsPopular { get; set; } = false;

        public List<string> Features { get; set; } = new List<string>();
    }
}
