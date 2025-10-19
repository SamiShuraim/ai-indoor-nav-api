namespace ai_indoor_nav_api.Models
{
    public class LevelUtilizationResponse
    {
        public Dictionary<int, LevelInfo> Levels { get; set; } = new();
    }

    public class LevelInfo
    {
        public int Level { get; set; }
        public int CurrentUtilization { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercentage { get; set; }
    }
}
