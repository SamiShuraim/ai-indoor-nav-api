namespace ai_indoor_nav_api.Models
{
    public class ControlTickResponse
    {
        public double Alpha1 { get; set; }
        public double AgeCutoff { get; set; }
        public double PDisabled { get; set; }
        public WindowInfo Window { get; set; } = new();
    }

    public class WindowInfo
    {
        public string Method { get; set; } = string.Empty;
        public double? SlidingWindowMinutes { get; set; }
        public double? HalfLifeMin { get; set; }
    }
}
