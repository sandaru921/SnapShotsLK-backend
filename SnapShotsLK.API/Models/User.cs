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
    }
}