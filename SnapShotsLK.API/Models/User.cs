using System.ComponentModel.DataAnnotations;

namespace SnapShotsLK.API.Models
{
    public class User
    {
        [Key] 
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; 

        public string Role { get; set; } = "Client"; // Default role is Client

        // Approval workflow
        public bool IsApproved { get; set; } = true; // Client users are auto-approved; admin set to false on register
        public string? RejectionReason { get; set; }

        // Proof document fields (for admin registration)
        public string? NicNumber { get; set; }
        public string? NicDocumentUrl { get; set; }      // Google Drive / hosted link
        public string? BusinessCertUrl { get; set; }     // Google Drive / hosted link
        public string? PortfolioUrl { get; set; }        // Portfolio link

        // Professional fields
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? ServiceType { get; set; }
        public string? BusinessName { get; set; }

        public ProfessionalProfile? ProfessionalProfile { get; set; }
    }
}