namespace SnapShotsLK.API.DTOs
{
    public class AdminApprovalDto
    {
        public int UserId { get; set; }

        /// <summary>"approve" or "reject"</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Required when Action is "reject"</summary>
        public string? RejectionReason { get; set; }
    }
}
