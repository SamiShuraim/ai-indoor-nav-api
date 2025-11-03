namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Configuration for capacity-based adaptive load balancer with soft gate.
    /// </summary>
    public class LoadBalancerConfig
    {
        // Capacity limits
        public int L1CapSoft { get; set; } = 500;
        public int L1CapHard { get; set; } = 550;
        public int L2Cap { get; set; } = 3000;
        public int L3Cap { get; set; } = 3000;

        // Dwell time
        public double DwellMinutes { get; set; } = 45.0;
        public double TtlBufferMinutes { get; set; } = 0.0;

        // Target alpha1 (steady-state share)
        public double TargetAlpha1 { get; set; } = 0.0769; // 500 / (500+3000+3000)
        public double Alpha1Min { get; set; } = 0.05;
        public double Alpha1Max { get; set; } = 0.12;

        // Controller
        public double TargetUtilL1 { get; set; } = 0.90;
        public double ControllerGain { get; set; } = 0.05;

        // Soft gate
        public bool SoftGateEnabled { get; set; } = true;
        public double SoftGateBandYears { get; set; } = 3.0;
        public double SoftGateFloor { get; set; } = 0.02;
        public double SoftGateCeiling { get; set; } = 0.98;

        // Recent share guard
        public double RecentShareWindowMinutes { get; set; } = 10.0;
        public double RecentShareGuard { get; set; } = 0.02;

        // Rolling window for statistics
        public double SlidingWindowMinutes { get; set; } = 45.0;

        public void Validate()
        {
            if (L1CapSoft <= 0 || L1CapHard <= 0 || L2Cap <= 0 || L3Cap <= 0)
                throw new ArgumentException("All capacity limits must be positive");
            
            if (L1CapHard < L1CapSoft)
                throw new ArgumentException("L1CapHard must be >= L1CapSoft");
            
            if (DwellMinutes <= 0)
                throw new ArgumentException("DwellMinutes must be positive");
            
            if (Alpha1Min < 0 || Alpha1Max > 1 || Alpha1Min > Alpha1Max)
                throw new ArgumentException("Invalid alpha1 bounds");
            
            if (TargetUtilL1 <= 0 || TargetUtilL1 > 1)
                throw new ArgumentException("TargetUtilL1 must be in (0, 1]");
            
            if (ControllerGain <= 0)
                throw new ArgumentException("ControllerGain must be positive");
        }
    }
}
