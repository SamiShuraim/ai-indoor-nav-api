using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Quantile-based occupancy load balancer.
    /// Uses dynamic age cutoffs computed from the distribution of recent arrivals.
    /// NO wait times - just occupancy tracking and distribution to minimize crowding.
    /// </summary>
    public class LoadBalancerService
    {
        private readonly object _lock = new object();
        private readonly LoadBalancerConfig _config;
        private readonly RollingQuantileEstimator _quantileEstimator;
        private readonly RollingCounts _rollingCounts;
        private readonly LevelTracker _levelTracker;

        public LoadBalancerService()
        {
            _config = new LoadBalancerConfig();
            _config.Validate();

            // Initialize components based on config
            bool useDecay = _config.WindowMode == "decay";
            double windowMinutes = _config.SlidingWindowMinutes;
            double halfLife = _config.HalfLifeMinutes;

            _quantileEstimator = new RollingQuantileEstimator(windowMinutes, useDecay, halfLife);
            _rollingCounts = new RollingCounts(windowMinutes, useDecay, halfLife);
            _levelTracker = new LevelTracker();
        }

        /// <summary>
        /// Assigns a pilgrim to a level based on age, disability status, and dynamic age cutoff.
        /// The age cutoff is computed from quantiles of recent arrivals.
        /// </summary>
        public ArrivalAssignResponse AssignArrival(ArrivalAssignRequest request)
        {
            lock (_lock)
            {
                // Validate age
                if (request.Age < 0 || request.Age > 120)
                {
                    throw new ArgumentException($"Age must be between 0 and 120, got {request.Age}");
                }

                // Record arrival in rolling statistics
                _rollingCounts.RecordArrival(request.IsDisabled);
                
                // Only track ages for non-disabled pilgrims (for quantile calculation)
                if (!request.IsDisabled)
                {
                    _quantileEstimator.Insert(request.Age);
                }

                // Get current occupancy at each level
                var occupancy = _levelTracker.GetAllQueueLengths();
                int level1Count = occupancy.GetValueOrDefault(1, 0);
                int level2Count = occupancy.GetValueOrDefault(2, 0);
                int level3Count = occupancy.GetValueOrDefault(3, 0);

                // Compute dynamic age cutoff
                double pDisabled = _rollingCounts.GetPDisabled();
                double shareLeftForOld = Math.Max(0, _config.Alpha1 - pDisabled);
                double tau = 1.0 - shareLeftForOld;
                tau = Math.Max(0, Math.Min(1, tau));
                double ageCutoff = _quantileEstimator.GetQuantile(tau);

                // Assign level using dynamic cutoff
                int assignedLevel;
                string reason;

                // Rule 1: Disabled always go to Level 1
                if (request.IsDisabled)
                {
                    assignedLevel = 1;
                    reason = "Disabled pilgrim prioritized for Level 1";
                }
                // Rule 2: Age >= dynamic cutoff goes to Level 1 (but check occupancy to avoid extreme crowding)
                else if (ageCutoff != double.NegativeInfinity && request.Age >= ageCutoff)
                {
                    // Check if Level 1 is severely overcrowded compared to others
                    int avgOthers = (level2Count + level3Count) / 2;
                    bool level1Overcrowded = level1Count > avgOthers * 1.5 && level1Count > 20; // 50% more crowded AND has significant people
                    
                    if (level1Overcrowded)
                    {
                        assignedLevel = level2Count <= level3Count ? 2 : 3;
                        reason = $"Age {request.Age} >= cutoff ({ageCutoff:F1}), but Level 1 severely overcrowded; redirected to Level {assignedLevel}";
                    }
                    else
                    {
                        assignedLevel = 1;
                        reason = $"Age {request.Age} >= dynamic cutoff ({ageCutoff:F1})";
                    }
                }
                // Rule 3: Below cutoff goes to less crowded of Level 2/3
                else
                {
                    assignedLevel = level2Count <= level3Count ? 2 : 3;
                    reason = ageCutoff == double.NegativeInfinity 
                        ? "No age history yet; non-disabled assigned to less crowded level"
                        : $"Age {request.Age} < cutoff ({ageCutoff:F1}); assigned to less crowded level";
                }

                // Record arrival (pilgrim enters immediately and stays for 45 minutes)
                _levelTracker.RecordArrival(assignedLevel);

                // Get updated occupancy
                occupancy = _levelTracker.GetAllQueueLengths();

                // Generate trace ID
                string traceId = Guid.NewGuid().ToString();

                return new ArrivalAssignResponse
                {
                    Level = assignedLevel,
                    Decision = new DecisionInfo
                    {
                        IsDisabled = request.IsDisabled,
                        Age = request.Age,
                        AgeCutoff = ageCutoff == double.NegativeInfinity ? 0 : ageCutoff,
                        Alpha1 = _config.Alpha1,
                        PDisabled = pDisabled,
                        ShareLeftForOld = shareLeftForOld,
                        TauQuantile = tau,
                        WaitEst = new Dictionary<int, double>
                        {
                            [1] = occupancy.GetValueOrDefault(1, 0),
                            [2] = occupancy.GetValueOrDefault(2, 0),
                            [3] = occupancy.GetValueOrDefault(3, 0)
                        },
                        Reason = reason
                    },
                    TraceId = traceId
                };
            }
        }

        /// <summary>
        /// Gets current metrics including occupancy and dynamic age cutoff.
        /// </summary>
        public MetricsResponse GetMetrics()
        {
            lock (_lock)
            {
                var occupancy = _levelTracker.GetAllQueueLengths();
                var (total, disabled, nonDisabled) = _rollingCounts.GetCounts();
                double pDisabled = _rollingCounts.GetPDisabled();
                
                double shareLeftForOld = Math.Max(0, _config.Alpha1 - pDisabled);
                double tau = 1.0 - shareLeftForOld;
                tau = Math.Max(0, Math.Min(1, tau));
                double ageCutoff = _quantileEstimator.GetQuantile(tau);

                var response = new MetricsResponse
                {
                    Alpha1 = _config.Alpha1,
                    Alpha1Min = _config.Alpha1Min,
                    Alpha1Max = _config.Alpha1Max,
                    WaitTargetMinutes = 0, // Not used
                    ControllerGain = 0, // Not used
                    PDisabled = pDisabled,
                    AgeCutoff = ageCutoff == double.NegativeInfinity ? 0 : ageCutoff,
                    Counts = new CountsInfo
                    {
                        Total = total,
                        Disabled = disabled,
                        NonDisabled = nonDisabled
                    },
                    Levels = occupancy.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new LevelMetrics
                        {
                            WaitEst = kvp.Value, // Actually occupancy count (kept for backward compatibility)
                            QueueLength = kvp.Value,
                            ThroughputPerMin = 0 // Not tracked
                        }
                    )
                };

                // Add quantiles if we have data
                if (_quantileEstimator.Count > 0)
                {
                    response.QuantilesNonDisabledAge = new Dictionary<string, double>
                    {
                        ["q50"] = _quantileEstimator.GetQuantile(0.5),
                        ["q80"] = _quantileEstimator.GetQuantile(0.8),
                        ["q90"] = _quantileEstimator.GetQuantile(0.9)
                    };
                }

                return response;
            }
        }

        /// <summary>
        /// Updates configuration at runtime.
        /// </summary>
        public ConfigResponse UpdateConfig(ConfigUpdateRequest request)
        {
            lock (_lock)
            {
                if (request.Alpha1.HasValue)
                    _config.Alpha1 = request.Alpha1.Value;
                
                if (request.Alpha1Min.HasValue)
                    _config.Alpha1Min = request.Alpha1Min.Value;
                
                if (request.Alpha1Max.HasValue)
                    _config.Alpha1Max = request.Alpha1Max.Value;

                if (request.Window != null)
                {
                    if (request.Window.Mode != null)
                        _config.WindowMode = request.Window.Mode;
                    
                    if (request.Window.Minutes.HasValue)
                        _config.SlidingWindowMinutes = request.Window.Minutes.Value;
                    
                    if (request.Window.HalfLifeMinutes.HasValue)
                        _config.HalfLifeMinutes = request.Window.HalfLifeMinutes.Value;
                }

                // Validate updated config
                _config.Validate();

                return GetCurrentConfig();
            }
        }

        /// <summary>
        /// Gets the current configuration.
        /// </summary>
        public ConfigResponse GetCurrentConfig()
        {
            return new ConfigResponse
            {
                AgeThreshold = 0, // Not used - we use dynamic cutoff
                Level1TargetShare = _config.Alpha1,
                Alpha1 = _config.Alpha1,
                Alpha1Min = _config.Alpha1Min,
                Alpha1Max = _config.Alpha1Max,
                WaitTargetMinutes = 0, // Not used
                ControllerGain = 0, // Not used
                Window = new WindowConfig
                {
                    Mode = _config.WindowMode,
                    Minutes = _config.SlidingWindowMinutes,
                    HalfLifeMinutes = _config.HalfLifeMinutes
                },
                SoftGate = null, // Not used
                Randomization = null // Not used
            };
        }

        // Legacy/no-op methods for backward compatibility
        public void UpdateLevelState(LevelStateUpdateRequest request)
        {
            // No-op: We don't track wait times
        }

        public ControlTickResponse PerformControlTick()
        {
            lock (_lock)
            {
                double pDisabled = _rollingCounts.GetPDisabled();
                double shareLeftForOld = Math.Max(0, _config.Alpha1 - pDisabled);
                double tau = 1.0 - shareLeftForOld;
                tau = Math.Max(0, Math.Min(1, tau));
                double ageCutoff = _quantileEstimator.GetQuantile(tau);

                return new ControlTickResponse
                {
                    Alpha1 = _config.Alpha1,
                    AgeCutoff = ageCutoff == double.NegativeInfinity ? 0 : ageCutoff,
                    PDisabled = pDisabled,
                    Window = new WindowInfo
                    {
                        Method = _config.WindowMode,
                        SlidingWindowMinutes = _config.WindowMode == "sliding" ? _config.SlidingWindowMinutes : null,
                        HalfLifeMin = _config.WindowMode == "decay" ? _config.HalfLifeMinutes : null
                    }
                };
            }
        }

        #region Legacy Support

        public LevelAssignmentResponse AssignLevel(LevelAssignmentRequest request)
        {
            // Map old request to new format
            var newRequest = new ArrivalAssignRequest
            {
                Age = request.Age,
                IsDisabled = !request.IsHealthy // isHealthy -> isDisabled (inverse)
            };

            var response = AssignArrival(newRequest);

            // Map back to old response format
            return new LevelAssignmentResponse
            {
                AssignedLevel = response.Level,
                CurrentUtilization = 0,
                Capacity = 0,
                UtilizationPercentage = 0
            };
        }

        public LevelUtilizationResponse GetUtilization()
        {
            var occupancy = _levelTracker.GetAllQueueLengths();
            
            return new LevelUtilizationResponse
            {
                Levels = new Dictionary<int, LevelInfo>
                {
                    [1] = new LevelInfo { Level = 1, CurrentUtilization = occupancy.GetValueOrDefault(1, 0), Capacity = 0, UtilizationPercentage = 0 },
                    [2] = new LevelInfo { Level = 2, CurrentUtilization = occupancy.GetValueOrDefault(2, 0), Capacity = 0, UtilizationPercentage = 0 },
                    [3] = new LevelInfo { Level = 3, CurrentUtilization = occupancy.GetValueOrDefault(3, 0), Capacity = 0, UtilizationPercentage = 0 }
                }
            };
        }

        public void ResetUtilization()
        {
            // No-op
        }

        #endregion
    }
}
