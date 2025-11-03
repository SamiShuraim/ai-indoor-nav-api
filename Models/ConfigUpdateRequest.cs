namespace ai_indoor_nav_api.Models
{
    public class ConfigUpdateRequest
    {
        public double? Alpha1 { get; set; }
        public double? Alpha1Min { get; set; }
        public double? Alpha1Max { get; set; }
        public WindowConfig? Window { get; set; }
    }

    public class WindowConfig
    {
        public string? Mode { get; set; }
        public double? Minutes { get; set; }
        public double? HalfLifeMinutes { get; set; }
    }

    public class ConfigResponse
    {
        public double Alpha1 { get; set; }
        public double Alpha1Min { get; set; }
        public double Alpha1Max { get; set; }
        public WindowConfig? Window { get; set; }
    }
}
