namespace ai_indoor_nav_api.Models
{
    /// <summary>
    /// Represents a visitor with their assigned information
    /// </summary>
    public class Visitor
    {
        public string VisitorId { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsDisabled { get; set; }
        public int AssignedLevel { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Response for visitor lookup
    /// </summary>
    public class VisitorInfoResponse
    {
        public string VisitorId { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Status { get; set; } = string.Empty; // "Disabled" or "Non-Disabled"
        public int AssignedLevel { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired { get; set; }
    }
}
