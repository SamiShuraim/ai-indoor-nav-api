namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Configuration for the adaptive load balancer.
    /// All parameters are runtime configurable.
    /// </summary>
    public class LoadBalancerConfig
    {
        // Controller parameters
        public double Alpha1 { get; set; } = 0.35;
        public double Alpha1Min { get; set; } = 0.15;
        public double Alpha1Max { get; set; } = 0.55;
        public double WaitTargetMinutes { get; set; } = 12.0;
        public double ControllerGain { get; set; } = 0.03;

        // Window parameters
        public string WindowMode { get; set; } = "sliding"; // "sliding" or "decay"
        public double SlidingWindowMinutes { get; set; } = 45.0;
        public double HalfLifeMinutes { get; set; } = 45.0;

        // Soft gate parameters
        public bool SoftGateEnabled { get; set; } = true;
        public double SoftGateBandYears { get; set; } = 3.0;

        // Randomization parameters
        public bool RandomizationEnabled { get; set; } = true;
        public double RandomizationRate { get; set; } = 0.07;

        public void Validate()
        {
            if (Alpha1 < 0 || Alpha1 > 1)
                throw new ArgumentException("Alpha1 must be between 0 and 1");
            
            if (Alpha1Min < 0 || Alpha1Min > 1)
                throw new ArgumentException("Alpha1Min must be between 0 and 1");
            
            if (Alpha1Max < 0 || Alpha1Max > 1)
                throw new ArgumentException("Alpha1Max must be between 0 and 1");
            
            if (Alpha1Min > Alpha1Max)
                throw new ArgumentException("Alpha1Min must be less than or equal to Alpha1Max");
            
            if (WaitTargetMinutes <= 0)
                throw new ArgumentException("WaitTargetMinutes must be positive");
            
            if (ControllerGain <= 0)
                throw new ArgumentException("ControllerGain must be positive");
            
            if (SlidingWindowMinutes <= 0)
                throw new ArgumentException("SlidingWindowMinutes must be positive");
            
            if (HalfLifeMinutes <= 0)
                throw new ArgumentException("HalfLifeMinutes must be positive");
            
            if (WindowMode != "sliding" && WindowMode != "decay")
                throw new ArgumentException("WindowMode must be 'sliding' or 'decay'");
            
            if (SoftGateBandYears < 0)
                throw new ArgumentException("SoftGateBandYears must be non-negative");
            
            if (RandomizationRate < 0 || RandomizationRate > 1)
                throw new ArgumentException("RandomizationRate must be between 0 and 1");
        }

        public LoadBalancerConfig Clone()
        {
            return new LoadBalancerConfig
            {
                Alpha1 = this.Alpha1,
                Alpha1Min = this.Alpha1Min,
                Alpha1Max = this.Alpha1Max,
                WaitTargetMinutes = this.WaitTargetMinutes,
                ControllerGain = this.ControllerGain,
                WindowMode = this.WindowMode,
                SlidingWindowMinutes = this.SlidingWindowMinutes,
                HalfLifeMinutes = this.HalfLifeMinutes,
                SoftGateEnabled = this.SoftGateEnabled,
                SoftGateBandYears = this.SoftGateBandYears,
                RandomizationEnabled = this.RandomizationEnabled,
                RandomizationRate = this.RandomizationRate
            };
        }
    }
}
