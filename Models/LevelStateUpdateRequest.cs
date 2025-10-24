namespace ai_indoor_nav_api.Models
{
    public class LevelStateUpdateRequest
    {
        public List<LevelState> Levels { get; set; } = new();
    }

    public class LevelState
    {
        public int Level { get; set; }
        public double? WaitEst { get; set; }
        public int? QueueLen { get; set; }
        public double? ThroughputPerMin { get; set; }
    }

    public class LevelStateUpdateResponse
    {
        public bool Ok { get; set; }
    }
}
