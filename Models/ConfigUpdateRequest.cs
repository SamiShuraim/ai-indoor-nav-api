namespace ai_indoor_nav_api.Models
{
    public class ConfigUpdateRequest
    {
        // Simplified config - only what's actually used
        public int? AgeThreshold { get; set; }
        public double? Level1TargetShare { get; set; }

        // Legacy fields - kept for backward compatibility but ignored
        public double? Alpha1 { get; set; }
        public double? Alpha1Min { get; set; }
        public double? Alpha1Max { get; set; }
        public double? WaitTargetMinutes { get; set; }
        public double? ControllerGain { get; set; }
        public WindowConfig? Window { get; set; }
        public SoftGateConfig? SoftGate { get; set; }
        public RandomizationConfig? Randomization { get; set; }
    }

    public class WindowConfig
    {
        public string? Mode { get; set; }
        public double? Minutes { get; set; }
        public double? HalfLifeMinutes { get; set; }
    }

    public class SoftGateConfig
    {
        public bool? Enabled { get; set; }
        public double? BandYears { get; set; }
    }

    public class RandomizationConfig
    {
        public bool? Enabled { get; set; }
        public double? Rate { get; set; }
    }

    public class ConfigResponse
    {
        // Active config
        public int AgeThreshold { get; set; }
        public double Level1TargetShare { get; set; }

        // Legacy fields - kept for backward compatibility
        public double Alpha1 { get; set; }
        public double Alpha1Min { get; set; }
        public double Alpha1Max { get; set; }
        public double WaitTargetMinutes { get; set; }
        public double ControllerGain { get; set; }
        public WindowConfig? Window { get; set; }
        public SoftGateConfig? SoftGate { get; set; }
        public RandomizationConfig? Randomization { get; set; }
    }
}
