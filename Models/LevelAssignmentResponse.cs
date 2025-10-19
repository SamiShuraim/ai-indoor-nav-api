namespace ai_indoor_nav_api.Models
{
    public class LevelAssignmentResponse
    {
        public int AssignedLevel { get; set; }
        public int CurrentUtilization { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}
