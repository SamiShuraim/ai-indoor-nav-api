namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Implements the feedback controller that adjusts alpha1 (target fraction for Level 1)
    /// based on Level 1 wait time and computes the dynamic age cutoff.
    /// </summary>
    public class AdaptiveController
    {
        private readonly object _lock = new object();
        private readonly LoadBalancerConfig _config;
        private readonly RollingQuantileEstimator _quantileEstimator;
        private readonly RollingCounts _rollingCounts;
        private readonly LevelStateManager _levelStateManager;
        private readonly Random _random = new Random();

        private double _currentAlpha1;
        private double _currentAgeCutoff = double.NegativeInfinity;
        private List<TimestampedAssignment> _recentAssignments = new();

        public AdaptiveController(
            LoadBalancerConfig config,
            RollingQuantileEstimator quantileEstimator,
            RollingCounts rollingCounts,
            LevelStateManager levelStateManager)
        {
            _config = config;
            _quantileEstimator = quantileEstimator;
            _rollingCounts = rollingCounts;
            _levelStateManager = levelStateManager;
            _currentAlpha1 = config.Alpha1;
        }

        /// <summary>
        /// Runs the controller tick: updates alpha1 and computes new age cutoff.
        /// Should be called periodically (e.g., every minute).
        /// </summary>
        public void Tick()
        {
            lock (_lock)
            {
                // Get current Level 1 wait estimate
                double waitEst1 = _levelStateManager.GetWaitEstimate(1);

                // Get current p_disabled
                double pDisabled = _rollingCounts.GetPDisabled();

                // Controller update rule
                double error = _config.WaitTargetMinutes - waitEst1;
                double newAlpha1 = _currentAlpha1 + _config.ControllerGain * error;

                // Clip alpha1 to bounds
                double lowerBound = Math.Max(_config.Alpha1Min, pDisabled);
                newAlpha1 = Math.Max(lowerBound, Math.Min(_config.Alpha1Max, newAlpha1));

                _currentAlpha1 = newAlpha1;

                // Compute dynamic age cutoff
                double shareLeftForOld = Math.Max(0, _currentAlpha1 - pDisabled);
                double tau = 1.0 - shareLeftForOld;

                // Ensure tau is in valid range [0, 1]
                tau = Math.Max(0, Math.Min(1, tau));

                // Get age cutoff from quantile estimator
                _currentAgeCutoff = _quantileEstimator.GetQuantile(tau);

                // Clean old assignments (keep last 10 minutes for soft gate check)
                CleanOldAssignments();
            }
        }

        /// <summary>
        /// Routes a single arrival to a level.
        /// </summary>
        public (int level, string reason) RouteArrival(int age, bool isDisabled)
        {
            lock (_lock)
            {
                // Validate age
                if (age < 0 || age > 120)
                {
                    throw new ArgumentException($"Age must be between 0 and 120, got {age}");
                }

                var waitEst = _levelStateManager.GetAllWaitEstimates();
                double wait1 = waitEst.GetValueOrDefault(1, 12.0);
                double wait2 = waitEst.GetValueOrDefault(2, 12.0);
                double wait3 = waitEst.GetValueOrDefault(3, 12.0);

                int assignedLevel;
                string reason;

                // Rule 1: Disabled always go to Level 1
                if (isDisabled)
                {
                    assignedLevel = 1;
                    reason = "disabled pilgrim prioritized for Level 1";
                }
                // Rule 2: Age >= cutoff goes to Level 1 (with soft gate check)
                else if (age >= _currentAgeCutoff && _currentAgeCutoff != double.NegativeInfinity)
                {
                    // Check soft gate: avoid overshooting alpha1
                    bool shouldBypassLevel1 = false;
                    
                    if (_config.SoftGateEnabled)
                    {
                        double recentL1Share = GetRecentLevel1Share();
                        // If we're significantly over target, and age is in the boundary band
                        if (recentL1Share > _currentAlpha1 + 0.1 && 
                            age >= _currentAgeCutoff && 
                            age <= _currentAgeCutoff + _config.SoftGateBandYears)
                        {
                            shouldBypassLevel1 = true;
                        }
                    }

                    // Apply randomization in the boundary band
                    bool randomize = false;
                    if (_config.RandomizationEnabled && 
                        age >= _currentAgeCutoff && 
                        age <= _currentAgeCutoff + _config.SoftGateBandYears)
                    {
                        if (_random.NextDouble() < _config.RandomizationRate)
                        {
                            randomize = true;
                        }
                    }

                    if (shouldBypassLevel1 || randomize)
                    {
                        // Send to less busy of Level 2/3
                        assignedLevel = wait2 <= wait3 ? 2 : 3;
                        reason = shouldBypassLevel1 
                            ? "soft gate active: Level 1 share above target, borderline age redirected"
                            : "randomization applied in boundary band";
                    }
                    else
                    {
                        assignedLevel = 1;
                        reason = "age ≥ dynamic cutoff; Level 1 within target share";
                    }
                }
                // Rule 3: Below cutoff goes to less busy of Level 2/3
                else
                {
                    assignedLevel = wait2 <= wait3 ? 2 : 3;
                    reason = _currentAgeCutoff == double.NegativeInfinity 
                        ? "no age history yet; non-disabled assigned to less busy level"
                        : $"age < cutoff ({_currentAgeCutoff:F1}); assigned to less busy level";
                }

                // Record assignment for soft gate tracking
                RecordAssignment(assignedLevel);

                return (assignedLevel, reason);
            }
        }

        public double GetCurrentAlpha1() => _currentAlpha1;
        public double GetCurrentAgeCutoff() => _currentAgeCutoff;

        public (double alpha1, double ageCutoff, double pDisabled, double shareLeftForOld, double tau) GetControllerState()
        {
            lock (_lock)
            {
                double pDisabled = _rollingCounts.GetPDisabled();
                double shareLeftForOld = Math.Max(0, _currentAlpha1 - pDisabled);
                double tau = 1.0 - shareLeftForOld;
                tau = Math.Max(0, Math.Min(1, tau));

                return (_currentAlpha1, _currentAgeCutoff, pDisabled, shareLeftForOld, tau);
            }
        }

        private void RecordAssignment(int level)
        {
            _recentAssignments.Add(new TimestampedAssignment
            {
                Level = level,
                Timestamp = DateTime.UtcNow
            });
        }

        private double GetRecentLevel1Share()
        {
            // Look at last 5 minutes of assignments
            var now = DateTime.UtcNow;
            var recent = _recentAssignments.Where(a => (now - a.Timestamp).TotalMinutes <= 5).ToList();
            
            if (recent.Count == 0) return 0;
            
            return (double)recent.Count(a => a.Level == 1) / recent.Count;
        }

        private void CleanOldAssignments()
        {
            var now = DateTime.UtcNow;
            _recentAssignments = _recentAssignments
                .Where(a => (now - a.Timestamp).TotalMinutes <= 10)
                .ToList();
        }

        private class TimestampedAssignment
        {
            public int Level { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
