namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Simple configuration for occupancy-based load balancer.
    /// </summary>
    public class LoadBalancerConfig
    {
        /// <summary>
        /// Age threshold for prioritizing Level 1 (e.g., 60 means 60+ prefer Level 1).
        /// </summary>
        public int AgeThreshold { get; set; } = 60;

        /// <summary>
        /// Target share for Level 1 as a fraction (e.g., 0.4 = 40%).
        /// Level 1 will accept elderly pilgrims until it reaches this share of total occupancy.
        /// </summary>
        public double Level1TargetShare { get; set; } = 0.40;
    }
}
