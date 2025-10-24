using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Manages the current state of each level (wait time, queue length, throughput).
    /// </summary>
    public class LevelStateManager
    {
        private readonly object _lock = new object();
        private readonly Dictionary<int, LevelStateData> _states = new();

        public LevelStateManager()
        {
            // Initialize with default states for levels 1, 2, 3
            _states[1] = new LevelStateData();
            _states[2] = new LevelStateData();
            _states[3] = new LevelStateData();
        }

        public void UpdateLevelState(int level, double? waitEst, int? queueLen, double? throughputPerMin)
        {
            lock (_lock)
            {
                if (!_states.ContainsKey(level))
                {
                    _states[level] = new LevelStateData();
                }

                var state = _states[level];
                
                if (waitEst.HasValue)
                {
                    state.WaitEst = waitEst.Value;
                }
                else if (queueLen.HasValue && throughputPerMin.HasValue && throughputPerMin.Value > 0)
                {
                    // Derive wait_est from queue and throughput
                    state.WaitEst = queueLen.Value / Math.Max(throughputPerMin.Value, 0.001);
                }

                if (queueLen.HasValue)
                {
                    state.QueueLen = queueLen.Value;
                }

                if (throughputPerMin.HasValue)
                {
                    state.ThroughputPerMin = throughputPerMin.Value;
                }
            }
        }

        public double GetWaitEstimate(int level)
        {
            lock (_lock)
            {
                return _states.TryGetValue(level, out var state) ? state.WaitEst : 12.0; // Default to 12 minutes
            }
        }

        public Dictionary<int, double> GetAllWaitEstimates()
        {
            lock (_lock)
            {
                return _states.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.WaitEst);
            }
        }

        public LevelStateData GetState(int level)
        {
            lock (_lock)
            {
                return _states.TryGetValue(level, out var state) ? state.Clone() : new LevelStateData();
            }
        }

        public class LevelStateData
        {
            public double WaitEst { get; set; } = 12.0; // Default wait estimate
            public int QueueLen { get; set; } = 0;
            public double ThroughputPerMin { get; set; } = 10.0;

            public LevelStateData Clone()
            {
                return new LevelStateData
                {
                    WaitEst = this.WaitEst,
                    QueueLen = this.QueueLen,
                    ThroughputPerMin = this.ThroughputPerMin
                };
            }
        }
    }
}
