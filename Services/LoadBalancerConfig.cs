namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Configuration for quantile-based occupancy load balancer.
    /// </summary>
    public class LoadBalancerConfig
    {
        /// <summary>
        /// Target fraction of arrivals to assign to Level 1 (e.g., 0.35 = 35%).
        /// This controls the dynamic age cutoff.
        /// </summary>
        public double Alpha1 { get; set; } = 0.35;

        /// <summary>
        /// Minimum value for Alpha1.
        /// </summary>
        public double Alpha1Min { get; set; } = 0.15;

        /// <summary>
        /// Maximum value for Alpha1.
        /// </summary>
        public double Alpha1Max { get; set; } = 0.55;

        /// <summary>
        /// Rolling window duration in minutes for tracking arrivals.
        /// </summary>
        public double SlidingWindowMinutes { get; set; } = 45.0;

        /// <summary>
        /// Window mode: "sliding" or "decay".
        /// </summary>
        public string WindowMode { get; set; } = "sliding";

        /// <summary>
        /// Half-life in minutes for exponential decay mode.
        /// </summary>
        public double HalfLifeMinutes { get; set; } = 45.0;

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
            
            if (SlidingWindowMinutes <= 0)
                throw new ArgumentException("SlidingWindowMinutes must be positive");
            
            if (HalfLifeMinutes <= 0)
                throw new ArgumentException("HalfLifeMinutes must be positive");
            
            if (WindowMode != "sliding" && WindowMode != "decay")
                throw new ArgumentException("WindowMode must be 'sliding' or 'decay'");
        }
    }
}
