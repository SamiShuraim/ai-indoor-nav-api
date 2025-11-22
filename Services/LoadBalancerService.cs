using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Capacity-based adaptive load balancer with dynamic age cutoff, 
    /// utilization-based feedback controller, and sigmoid soft gate.
    /// </summary>
    public class LoadBalancerService
    {
        private readonly object _lock = new object();
        private readonly LoadBalancerConfig _config;
        private readonly AssignmentLog _assignmentLog;
        private readonly RollingQuantileEstimator _quantileEstimator;
        private readonly RollingCounts _rollingCounts;
        private readonly VisitorService _visitorService;
        private readonly Random _random;
        private System.Threading.Timer? _tickTimer;

        private double _alpha1; // Current target share for L1
        private double _alpha1Hat; // Actual recent share to L1
        private double _ageCutoff;

        public LoadBalancerService(VisitorService visitorService)
        {
            _config = new LoadBalancerConfig();
            _config.Validate();

            _assignmentLog = new AssignmentLog(_config.DwellMinutes, _config.TtlBufferMinutes);
            _quantileEstimator = new RollingQuantileEstimator(_config.SlidingWindowMinutes, useDecay: false, halfLifeMinutes: 45);
            _rollingCounts = new RollingCounts(_config.SlidingWindowMinutes, useDecay: false, halfLifeMinutes: 45);
            _visitorService = visitorService;
            _random = new Random(42); // Seeded for reproducibility

            _alpha1 = _config.TargetAlpha1;
            _alpha1Hat = 0.0;
            _ageCutoff = double.PositiveInfinity;

            // Run controller tick every minute
            StartPeriodicTick();
            
            // Run initial tick
            ControllerTick();
        }

        /// <summary>
        /// Assigns a pilgrim to a level using capacity-based logic with soft gate.
        /// </summary>
        public ArrivalAssignResponse AssignArrival(ArrivalAssignRequest request)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // Validate age
                if (request.Age < 0 || request.Age > 120)
                {
                    throw new ArgumentException($"Age must be between 0 and 120, got {request.Age}");
                }

                // Evict expired assignments
                _assignmentLog.EvictExpired(now);

                // Get current active counts
                var active = _assignmentLog.GetActiveCounts();
                int active1 = active[1];
                int active2 = active[2];
                int active3 = active[3];

                int assignedLevel;
                string reason;
                double pL1 = 0.0;

                // === CASE 1: DISABLED ===
                if (request.IsDisabled)
                {
                    if (active1 < _config.L1CapHard)
                    {
                        assignedLevel = 1;
                        reason = "disabled priority";
                    }
                    else
                    {
                        assignedLevel = GetLessFullLevel(active2, active3);
                        reason = "disabled overflow (hard cap)";
                    }
                }
                // === CASE 2: NON-DISABLED ===
                else
                {
                    // Check if L1 is at capacity
                    if (active1 >= _config.L1CapSoft)
                    {
                        assignedLevel = GetLessFullLevel(active2, active3);
                        reason = "L1 capacity guard";
                    }
                    // Check if we have age history
                    else if (_ageCutoff == double.PositiveInfinity)
                    {
                        assignedLevel = GetLessFullLevel(active2, active3);
                        reason = "no non-disabled history";
                    }
                    // Apply soft gate
                    else
                    {
                        pL1 = ComputeSoftGateProbability(request.Age);
                        
                        // Draw random number
                        double r = _random.NextDouble();
                        
                        if (r < pL1)
                        {
                            // Propose L1
                            if (active1 < _config.L1CapSoft)
                            {
                                assignedLevel = 1;
                                reason = $"soft-gate pass (p={pL1:F3}, r={r:F3})";
                            }
                            else
                            {
                                assignedLevel = GetLessFullLevel(active2, active3);
                                reason = $"soft-gate pass but L1 full";
                            }
                        }
                        else
                        {
                            // Propose L2/L3
                            assignedLevel = GetLessFullLevel(active2, active3);
                            reason = $"soft-gate reject (p={pL1:F3}, r={r:F3})";
                        }
                    }
                }

                // Record assignment
                _assignmentLog.Add(assignedLevel, request.Age, request.IsDisabled, now);
                
                // Create visitor record and get unique ID
                string visitorId = _visitorService.CreateVisitor(
                    request.Age, 
                    request.IsDisabled, 
                    assignedLevel, 
                    now, 
                    _config.DwellMinutes
                );
                
                // Update rolling statistics
                _rollingCounts.RecordArrival(request.IsDisabled);
                if (!request.IsDisabled)
                {
                    _quantileEstimator.Insert(request.Age);
                }

                // Update alpha1_hat (recent share)
                UpdateAlpha1Hat(now);

                // Get updated active counts
                active = _assignmentLog.GetActiveCounts();

                // Get current stats for response
                double pDisabled = _rollingCounts.GetPDisabled();
                var (_, disabled, nonDisabled) = _rollingCounts.GetCounts();

                return new ArrivalAssignResponse
                {
                    Level = assignedLevel,
                    VisitorId = visitorId,
                    Decision = new DecisionInfo
                    {
                        IsDisabled = request.IsDisabled,
                        Age = request.Age,
                        AgeCutoff = _ageCutoff == double.PositiveInfinity ? 0 : _ageCutoff,
                        Alpha1 = _alpha1,
                        PDisabled = pDisabled,
                        ShareLeftForOld = Math.Max(0, _alpha1 - pDisabled),
                        TauQuantile = _ageCutoff == double.PositiveInfinity ? 0 : (1.0 - Math.Max(0, _alpha1 - pDisabled)),
                        Occupancy = new Dictionary<int, int>
                        {
                            [1] = active[1],
                            [2] = active[2],
                            [3] = active[3]
                        },
                        Reason = reason
                    },
                    TraceId = Guid.NewGuid().ToString()
                };
            }
        }

        /// <summary>
        /// Computes soft gate probability using sigmoid function.
        /// </summary>
        private double ComputeSoftGateProbability(int age)
        {
            if (!_config.SoftGateEnabled)
            {
                return age >= _ageCutoff ? 1.0 : 0.0;
            }

            double gamma = _config.SoftGateBandYears;
            
            // Deterministic zones
            if (age >= _ageCutoff + 2 * gamma)
            {
                return 1.0;
            }
            if (age <= _ageCutoff - 2 * gamma)
            {
                return 0.0;
            }

            // Sigmoid in the band
            double z = (age - _ageCutoff) / gamma;
            double sigmoid = 1.0 / (1.0 + Math.Exp(-z));
            double p = Math.Max(_config.SoftGateFloor, Math.Min(_config.SoftGateCeiling, sigmoid));

            // Guard against overshooting recent share
            if (_alpha1Hat > _alpha1 + _config.RecentShareGuard)
            {
                p = Math.Min(p, 0.10); // Only clearly-older can slip in
            }

            return p;
        }

        /// <summary>
        /// Returns the less full level between L2 and L3.
        /// </summary>
        private int GetLessFullLevel(int active2, int active3)
        {
            return active2 <= active3 ? 2 : 3;
        }

        /// <summary>
        /// Updates alpha1_hat (recent share to L1) over the recent window.
        /// </summary>
        private void UpdateAlpha1Hat(DateTime now)
        {
            var recentAssignments = _assignmentLog.GetRecentAssignments(now, _config.RecentShareWindowMinutes);
            if (recentAssignments.Count == 0)
            {
                _alpha1Hat = 0.0;
                return;
            }

            int l1Count = recentAssignments.Count(a => a.Level == 1);
            _alpha1Hat = (double)l1Count / recentAssignments.Count;
        }

        /// <summary>
        /// Controller tick: adjusts alpha1 based on L1 utilization and capacity.
        /// </summary>
        private void ControllerTick()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // Evict expired assignments
                _assignmentLog.EvictExpired(now);

                // Get active counts
                var active = _assignmentLog.GetActiveCounts();
                int active1 = active[1];

                // Compute L1 utilization
                double util = (double)active1 / _config.L1CapSoft;

                // Get disability rate
                double pDisabled = _rollingCounts.GetPDisabled();

                // Capacity-aware adjustment: if we have plenty of room, be more aggressive
                double capacityAvailable = _config.L1CapSoft - active1;
                double capacityRatio = capacityAvailable / _config.L1CapSoft;
                
                // If we have lots of capacity (>30% free) and low utilization (<70%), increase alpha1 aggressively
                if (capacityRatio > 0.30 && util < 0.70)
                {
                    // Boost alpha1 to use available capacity
                    double targetAlpha1 = Math.Min(_config.Alpha1Max, pDisabled + 0.20); // Reserve 20% more for elderly
                    _alpha1 = _alpha1 + 0.1 * (targetAlpha1 - _alpha1); // Fast adjustment
                }
                else
                {
                    // Normal feedback controller
                    double error = _config.TargetUtilL1 - util;
                    _alpha1 = _alpha1 + _config.ControllerGain * error;
                }

                // Clamp alpha1 with intelligent lower bound
                double lowerBound = Math.Max(_config.Alpha1Min, pDisabled + 0.05); // Always leave 5% for elderly
                _alpha1 = Math.Max(lowerBound, Math.Min(_config.Alpha1Max, _alpha1));

                // Compute share left for old
                double shareLeftForOld = Math.Max(0, _alpha1 - pDisabled);
                double tau = 1.0 - shareLeftForOld;
                tau = Math.Max(0, Math.Min(1, tau));

                // Compute age cutoff
                var (_, _, nonDisabled) = _rollingCounts.GetCounts();
                if (nonDisabled == 0 || shareLeftForOld < 0.01)
                {
                    // If almost no room for elderly, set a reasonable cutoff (e.g., 70)
                    _ageCutoff = shareLeftForOld < 0.01 ? 70 : double.PositiveInfinity;
                }
                else
                {
                    _ageCutoff = _quantileEstimator.GetQuantile(tau);
                    if (_ageCutoff == double.NegativeInfinity)
                    {
                        _ageCutoff = double.PositiveInfinity;
                    }
                }

                // Update alpha1_hat
                UpdateAlpha1Hat(now);
            }
        }

        /// <summary>
        /// Gets current metrics.
        /// </summary>
        public MetricsResponse GetMetrics()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                _assignmentLog.EvictExpired(now);
                
                var active = _assignmentLog.GetActiveCounts();
                var (total, disabled, nonDisabled) = _rollingCounts.GetCounts();
                double pDisabled = _rollingCounts.GetPDisabled();
                
                double util = (double)active[1] / _config.L1CapSoft;

                var response = new MetricsResponse
                {
                    Alpha1 = _alpha1,
                    Alpha1Min = _config.Alpha1Min,
                    Alpha1Max = _config.Alpha1Max,
                    PDisabled = pDisabled,
                    AgeCutoff = _ageCutoff == double.PositiveInfinity ? 0 : _ageCutoff,
                    Counts = new CountsInfo
                    {
                        Total = total,
                        Disabled = disabled,
                        NonDisabled = nonDisabled
                    },
                    Levels = active.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new LevelMetrics
                        {
                            Occupancy = kvp.Value
                        }
                    )
                };

                // Add quantiles if we have data
                if (_quantileEstimator.Count > 0 && _ageCutoff != double.PositiveInfinity)
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
                    _config.TargetAlpha1 = request.Alpha1.Value;
                
                if (request.Alpha1Min.HasValue)
                    _config.Alpha1Min = request.Alpha1Min.Value;
                
                if (request.Alpha1Max.HasValue)
                    _config.Alpha1Max = request.Alpha1Max.Value;

                if (request.Window != null)
                {
                    if (request.Window.Minutes.HasValue)
                        _config.SlidingWindowMinutes = request.Window.Minutes.Value;
                }

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
                Alpha1 = _alpha1,
                Alpha1Min = _config.Alpha1Min,
                Alpha1Max = _config.Alpha1Max,
                Window = new WindowConfig
                {
                    Mode = "sliding",
                    Minutes = _config.SlidingWindowMinutes,
                    HalfLifeMinutes = null
                }
            };
        }

        private void StartPeriodicTick()
        {
            _tickTimer = new System.Threading.Timer(
                callback: _ =>
                {
                    try
                    {
                        ControllerTick();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in controller tick: {ex.Message}");
                    }
                },
                state: null,
                dueTime: TimeSpan.FromMinutes(1),
                period: TimeSpan.FromMinutes(1)
            );
        }
    }
}
