using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Simple occupancy-based load balancer.
    /// Distributes pilgrims across 3 levels to minimize crowding.
    /// No waiting times - people just walk in, perform ritual for 45 minutes, and leave.
    /// </summary>
    public class LoadBalancerService
    {
        private readonly object _lock = new object();
        private readonly LevelTracker _levelTracker;
        private readonly LoadBalancerConfig _config;

        public LoadBalancerService()
        {
            _levelTracker = new LevelTracker();
            _config = new LoadBalancerConfig();
        }

        /// <summary>
        /// Assigns a pilgrim to a level based on age, disability status, and current occupancy.
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

                // Get current occupancy at each level
                var occupancy = _levelTracker.GetAllQueueLengths();
                int level1Count = occupancy.GetValueOrDefault(1, 0);
                int level2Count = occupancy.GetValueOrDefault(2, 0);
                int level3Count = occupancy.GetValueOrDefault(3, 0);
                int totalCount = level1Count + level2Count + level3Count;

                // Assign level using simple rules
                int assignedLevel;
                string reason;

                // Rule 1: Disabled always go to Level 1
                if (request.IsDisabled)
                {
                    assignedLevel = 1;
                    reason = "Disabled pilgrims always assigned to Level 1";
                }
                // Rule 2: Older people (age >= threshold) prefer Level 1, but avoid overcrowding
                else if (request.Age >= _config.AgeThreshold)
                {
                    // Check if Level 1 is under its target share (default 40% to leave room for spikes)
                    if (totalCount == 0 || level1Count < totalCount * _config.Level1TargetShare)
                    {
                        assignedLevel = 1;
                        reason = $"Age {request.Age} >= {_config.AgeThreshold}, Level 1 under capacity";
                    }
                    else
                    {
                        // Level 1 over capacity, redirect to least crowded of 2/3
                        assignedLevel = level2Count <= level3Count ? 2 : 3;
                        reason = $"Age {request.Age} >= {_config.AgeThreshold} but Level 1 over capacity, redirected to Level {assignedLevel}";
                    }
                }
                // Rule 3: Younger people go to least crowded of Level 2 or 3
                else
                {
                    assignedLevel = level2Count <= level3Count ? 2 : 3;
                    reason = $"Age {request.Age} < {_config.AgeThreshold}, assigned to least crowded level";
                }

                // Record arrival (pilgrim enters immediately and stays for 45 minutes)
                _levelTracker.RecordArrival(assignedLevel);

                // Get updated occupancy
                occupancy = _levelTracker.GetAllQueueLengths();

                return new ArrivalAssignResponse
                {
                    Level = assignedLevel,
                    Decision = new DecisionInfo
                    {
                        IsDisabled = request.IsDisabled,
                        Age = request.Age,
                        AgeCutoff = _config.AgeThreshold,
                        Alpha1 = 0, // Not used anymore
                        PDisabled = 0, // Not used anymore
                        ShareLeftForOld = 0, // Not used anymore
                        TauQuantile = 0, // Not used anymore
                        WaitEst = new Dictionary<int, double>
                        {
                            [1] = occupancy.GetValueOrDefault(1, 0),
                            [2] = occupancy.GetValueOrDefault(2, 0),
                            [3] = occupancy.GetValueOrDefault(3, 0)
                        },
                        Reason = reason
                    },
                    TraceId = Guid.NewGuid().ToString()
                };
            }
        }

        /// <summary>
        /// Gets current metrics (occupancy at each level).
        /// </summary>
        public MetricsResponse GetMetrics()
        {
            lock (_lock)
            {
                var occupancy = _levelTracker.GetAllQueueLengths();

                return new MetricsResponse
                {
                    Alpha1 = 0, // Not used
                    Alpha1Min = 0, // Not used
                    Alpha1Max = 0, // Not used
                    WaitTargetMinutes = 0, // Not used
                    ControllerGain = 0, // Not used
                    PDisabled = 0, // Not used
                    AgeCutoff = _config.AgeThreshold,
                    Counts = new CountsInfo
                    {
                        Total = occupancy.Values.Sum(),
                        Disabled = 0, // Not tracked
                        NonDisabled = 0 // Not tracked
                    },
                    Levels = occupancy.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new LevelMetrics
                        {
                            WaitEst = kvp.Value, // Now represents occupancy count
                            QueueLength = kvp.Value,
                            ThroughputPerMin = 0 // Not tracked
                        }
                    )
                };
            }
        }

        /// <summary>
        /// Updates configuration at runtime.
        /// </summary>
        public ConfigResponse UpdateConfig(ConfigUpdateRequest request)
        {
            lock (_lock)
            {
                if (request.AgeThreshold.HasValue)
                {
                    if (request.AgeThreshold.Value < 0 || request.AgeThreshold.Value > 120)
                    {
                        throw new ArgumentException("AgeThreshold must be between 0 and 120");
                    }
                    _config.AgeThreshold = request.AgeThreshold.Value;
                }

                if (request.Level1TargetShare.HasValue)
                {
                    if (request.Level1TargetShare.Value < 0 || request.Level1TargetShare.Value > 1)
                    {
                        throw new ArgumentException("Level1TargetShare must be between 0 and 1");
                    }
                    _config.Level1TargetShare = request.Level1TargetShare.Value;
                }

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
                AgeThreshold = _config.AgeThreshold,
                Level1TargetShare = _config.Level1TargetShare,
                Alpha1 = 0, // Not used
                Alpha1Min = 0, // Not used
                Alpha1Max = 0, // Not used
                WaitTargetMinutes = 0, // Not used
                ControllerGain = 0, // Not used
                Window = null, // Not used
                SoftGate = null, // Not used
                Randomization = null // Not used
            };
        }

        // These methods are no longer needed but kept for backward compatibility
        public void UpdateLevelState(LevelStateUpdateRequest request)
        {
            // No-op: We don't track wait times anymore
        }

        public ControlTickResponse PerformControlTick()
        {
            // No-op: No controller to tick
            return new ControlTickResponse
            {
                Alpha1 = 0,
                AgeCutoff = _config.AgeThreshold,
                PDisabled = 0,
                Window = null
            };
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
