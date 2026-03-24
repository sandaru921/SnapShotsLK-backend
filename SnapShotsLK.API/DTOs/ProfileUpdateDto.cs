using System.Collections.Generic;

namespace SnapShotsLK.API.DTOs
{
    public class PackageDto
    {
        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public bool IsPopular { get; set; } = false;
        public List<string> Features { get; set; } = new List<string>();
    }

    public class ProfileUpdateDto
    {
        public string Bio { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;
        public string Experience { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string ResponseTime { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;
        
        public string InstagramUrl { get; set; } = string.Empty;
        public string FacebookUrl { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;

        public List<string> Specialties { get; set; } = new List<string>();
        public List<string> PortfolioUrls { get; set; } = new List<string>();
        public List<string> Languages { get; set; } = new List<string>();
        public List<string> Achievements { get; set; } = new List<string>();

        public List<PackageDto> Packages { get; set; } = new List<PackageDto>();
    }
}
