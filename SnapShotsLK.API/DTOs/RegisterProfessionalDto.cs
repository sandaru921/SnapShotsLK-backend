using System.ComponentModel.DataAnnotations;

namespace SnapShotsLK.API.DTOs
{
    public class RegisterProfessionalDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "pending_admin";

        [Required]
        public string ServiceType { get; set; } = string.Empty;

        public string BusinessName { get; set; } = string.Empty;

        // Proof documents (URLs / links)
        [Required]
        public string NicNumber { get; set; } = string.Empty;

        [Required]
        public string NicDocumentUrl { get; set; } = string.Empty;

        public string? BusinessCertUrl { get; set; }

        public string? PortfolioUrl { get; set; }
    }
}
