using System.Collections.Concurrent;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Adaptive load balancer service that assigns pilgrims to levels based on
    /// age, disability status, and real-time congestion data.
    /// Uses a feedback controller with rolling statistics and dynamic age cutoffs.
    /// </summary>
    public class LoadBalancerService
    {
        private readonly object _lock = new object();
        private readonly LoadBalancerConfig _config;
        private readonly RollingQuantileEstimator _quantileEstimator;
        private readonly RollingCounts _rollingCounts;
        private readonly LevelStateManager _levelStateManager;
        private readonly AdaptiveController _controller;
        private readonly LevelTracker _levelTracker;
        private System.Threading.Timer? _tickTimer;

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
            _levelStateManager = new LevelStateManager();
            _levelTracker = new LevelTracker();
            _controller = new AdaptiveController(_config, _quantileEstimator, _rollingCounts, _levelStateManager);

            // Start periodic controller tick (every minute)
            StartPeriodicTick();
        }

        /// <summary>
        /// Assigns a pilgrim to a level based on age and disability status.
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
                
                // Only track ages for non-disabled pilgrims
                if (!request.IsDisabled)
                {
                    _quantileEstimator.Insert(request.Age);
                }

                // Route the arrival
                var (level, reason) = _controller.RouteArrival(request.Age, request.IsDisabled);

                // Record arrival at the assigned level (pilgrim arrives immediately)
                _levelTracker.RecordArrival(level);

                // Get controller state for response
                var (alpha1, ageCutoff, pDisabled, shareLeftForOld, tau) = _controller.GetControllerState();
                var waitEst = _levelStateManager.GetAllWaitEstimates();

                // Generate trace ID
                string traceId = Guid.NewGuid().ToString();

                return new ArrivalAssignResponse
                {
                    Level = level,
                    Decision = new DecisionInfo
                    {
                        IsDisabled = request.IsDisabled,
                        Age = request.Age,
                        AgeCutoff = ageCutoff == double.NegativeInfinity ? 0 : ageCutoff,
                        Alpha1 = alpha1,
                        PDisabled = pDisabled,
                        ShareLeftForOld = shareLeftForOld,
                        TauQuantile = tau,
                        WaitEst = waitEst,
                        Reason = reason
                    },
                    TraceId = traceId
                };
            }
        }

        /// <summary>
        /// Updates the state of one or more levels (wait times, queue lengths, throughput).
        /// </summary>
        public void UpdateLevelState(LevelStateUpdateRequest request)
        {
            foreach (var levelState in request.Levels)
            {
                _levelStateManager.UpdateLevelState(
                    levelState.Level,
                    levelState.WaitEst,
                    levelState.QueueLen,
                    levelState.ThroughputPerMin
                );
            }
        }

        /// <summary>
        /// Manually triggers a controller tick (normally happens automatically every minute).
        /// </summary>
        public ControlTickResponse PerformControlTick()
        {
            lock (_lock)
            {
                // Update level states from tracker before running controller tick
                UpdateLevelStatesFromTracker();
                
                // Run controller tick
                _controller.Tick();

                var (alpha1, ageCutoff, pDisabled, _, _) = _controller.GetControllerState();

                return new ControlTickResponse
                {
                    Alpha1 = alpha1,
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

        /// <summary>
        /// Gets current metrics and statistics.
        /// </summary>
        public MetricsResponse GetMetrics()
        {
            lock (_lock)
            {
                var (alpha1, ageCutoff, pDisabled, _, _) = _controller.GetControllerState();
                var (total, disabled, nonDisabled) = _rollingCounts.GetCounts();
                var waitEst = _levelStateManager.GetAllWaitEstimates();
                var levelStats = _levelTracker.GetAllLevelStats();

                var response = new MetricsResponse
                {
                    Alpha1 = alpha1,
                    Alpha1Min = _config.Alpha1Min,
                    Alpha1Max = _config.Alpha1Max,
                    WaitTargetMinutes = _config.WaitTargetMinutes,
                    ControllerGain = _config.ControllerGain,
                    PDisabled = pDisabled,
                    AgeCutoff = ageCutoff == double.NegativeInfinity ? 0 : ageCutoff,
                    Counts = new CountsInfo
                    {
                        Total = total,
                        Disabled = disabled,
                        NonDisabled = nonDisabled
                    },
                    Levels = waitEst.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new LevelMetrics 
                        { 
                            WaitEst = kvp.Value,
                            QueueLength = levelStats.ContainsKey(kvp.Key) ? levelStats[kvp.Key].QueueLength : 0,
                            ThroughputPerMin = levelStats.ContainsKey(kvp.Key) ? levelStats[kvp.Key].ThroughputPerMinute : 0
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
                
                if (request.WaitTargetMinutes.HasValue)
                    _config.WaitTargetMinutes = request.WaitTargetMinutes.Value;
                
                if (request.ControllerGain.HasValue)
                    _config.ControllerGain = request.ControllerGain.Value;

                if (request.Window != null)
                {
                    if (request.Window.Mode != null)
                        _config.WindowMode = request.Window.Mode;
                    
                    if (request.Window.Minutes.HasValue)
                        _config.SlidingWindowMinutes = request.Window.Minutes.Value;
                    
                    if (request.Window.HalfLifeMinutes.HasValue)
                        _config.HalfLifeMinutes = request.Window.HalfLifeMinutes.Value;
                }

                if (request.SoftGate != null)
                {
                    if (request.SoftGate.Enabled.HasValue)
                        _config.SoftGateEnabled = request.SoftGate.Enabled.Value;
                    
                    if (request.SoftGate.BandYears.HasValue)
                        _config.SoftGateBandYears = request.SoftGate.BandYears.Value;
                }

                if (request.Randomization != null)
                {
                    if (request.Randomization.Enabled.HasValue)
                        _config.RandomizationEnabled = request.Randomization.Enabled.Value;
                    
                    if (request.Randomization.Rate.HasValue)
                        _config.RandomizationRate = request.Randomization.Rate.Value;
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
                Alpha1 = _config.Alpha1,
                Alpha1Min = _config.Alpha1Min,
                Alpha1Max = _config.Alpha1Max,
                WaitTargetMinutes = _config.WaitTargetMinutes,
                ControllerGain = _config.ControllerGain,
                Window = new WindowConfig
                {
                    Mode = _config.WindowMode,
                    Minutes = _config.SlidingWindowMinutes,
                    HalfLifeMinutes = _config.HalfLifeMinutes
                },
                SoftGate = new SoftGateConfig
                {
                    Enabled = _config.SoftGateEnabled,
                    BandYears = _config.SoftGateBandYears
                },
                Randomization = new RandomizationConfig
                {
                    Enabled = _config.RandomizationEnabled,
                    Rate = _config.RandomizationRate
                }
            };
        }

        private void StartPeriodicTick()
        {
            // Run controller tick every minute
            _tickTimer = new System.Threading.Timer(
                callback: _ =>
                {
                    try
                    {
                        lock (_lock)
                        {
                            // Update level states from tracker before running controller tick
                            UpdateLevelStatesFromTracker();
                            
                            // Run controller tick to adjust alpha1 and age cutoff
                            _controller.Tick();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't crash the service
                        Console.WriteLine($"Error in controller tick: {ex.Message}");
                    }
                },
                state: null,
                dueTime: TimeSpan.FromMinutes(1),
                period: TimeSpan.FromMinutes(1)
            );
        }

        /// <summary>
        /// Updates the LevelStateManager with current statistics from the LevelTracker.
        /// This is called automatically during each controller tick.
        /// </summary>
        private void UpdateLevelStatesFromTracker()
        {
            var stats = _levelTracker.GetAllLevelStats();
            foreach (var kvp in stats)
            {
                int level = kvp.Key;
                var levelStats = kvp.Value;

                _levelStateManager.UpdateLevelState(
                    level,
                    levelStats.EstimatedWaitMinutes,
                    levelStats.QueueLength,
                    levelStats.ThroughputPerMinute
                );
            }
        }

        // Legacy methods for backward compatibility (if needed)
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

            // Map back to old response format (simplified)
            return new LevelAssignmentResponse
            {
                AssignedLevel = response.Level,
                CurrentUtilization = 0, // Not tracked in new system
                Capacity = 0,
                UtilizationPercentage = 0
            };
        }

        public LevelUtilizationResponse GetUtilization()
        {
            // Return empty response as we don't track utilization the same way
            return new LevelUtilizationResponse
            {
                Levels = new Dictionary<int, LevelInfo>
                {
                    [1] = new LevelInfo { Level = 1, CurrentUtilization = 0, Capacity = 0, UtilizationPercentage = 0 },
                    [2] = new LevelInfo { Level = 2, CurrentUtilization = 0, Capacity = 0, UtilizationPercentage = 0 },
                    [3] = new LevelInfo { Level = 3, CurrentUtilization = 0, Capacity = 0, UtilizationPercentage = 0 }
                }
            };
        }

        public void ResetUtilization()
        {
            // Not applicable in new system
        }

        #endregion
    }
}
