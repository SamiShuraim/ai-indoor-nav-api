namespace ai_indoor_nav_api.Models
{
    public class MetricsResponse
    {
        public double Alpha1 { get; set; }
        public double Alpha1Min { get; set; }
        public double Alpha1Max { get; set; }
        public double WaitTargetMinutes { get; set; }
        public double ControllerGain { get; set; }
        public double PDisabled { get; set; }
        public double AgeCutoff { get; set; }
        public CountsInfo Counts { get; set; } = new();
        public Dictionary<string, double> QuantilesNonDisabledAge { get; set; } = new();
        public Dictionary<int, LevelMetrics> Levels { get; set; } = new();
    }

    public class CountsInfo
    {
        public int Total { get; set; }
        public int Disabled { get; set; }
        public int NonDisabled { get; set; }
    }

    public class LevelMetrics
    {
        public double WaitEst { get; set; }
        public int QueueLength { get; set; }
        public double ThroughputPerMin { get; set; }
    }
}
