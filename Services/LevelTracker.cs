namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Tracks active pilgrims at each level and automatically manages their lifecycle.
    /// Pilgrims are assumed to arrive immediately upon assignment and leave after 45 minutes.
    /// </summary>
    public class LevelTracker
    {
        private readonly object _lock = new object();
        private readonly Dictionary<int, List<PilgrimArrival>> _activePilgrims = new();
        private const double DEFAULT_DURATION_MINUTES = 45.0;

        public LevelTracker()
        {
            // Initialize tracking for levels 1, 2, 3
            _activePilgrims[1] = new List<PilgrimArrival>();
            _activePilgrims[2] = new List<PilgrimArrival>();
            _activePilgrims[3] = new List<PilgrimArrival>();
        }

        /// <summary>
        /// Records that a pilgrim has arrived at the specified level.
        /// </summary>
        public void RecordArrival(int level)
        {
            lock (_lock)
            {
                if (!_activePilgrims.ContainsKey(level))
                {
                    _activePilgrims[level] = new List<PilgrimArrival>();
                }

                _activePilgrims[level].Add(new PilgrimArrival
                {
                    ArrivalTime = DateTime.UtcNow,
                    DepartureTime = DateTime.UtcNow.AddMinutes(DEFAULT_DURATION_MINUTES)
                });

                // Clean up departed pilgrims
                CleanupDepartedPilgrims(level);
            }
        }

        /// <summary>
        /// Gets the current queue length at the specified level.
        /// </summary>
        public int GetQueueLength(int level)
        {
            lock (_lock)
            {
                CleanupDepartedPilgrims(level);
                return _activePilgrims.TryGetValue(level, out var pilgrims) ? pilgrims.Count : 0;
            }
        }

        /// <summary>
        /// Gets queue lengths for all levels.
        /// </summary>
        public Dictionary<int, int> GetAllQueueLengths()
        {
            lock (_lock)
            {
                var result = new Dictionary<int, int>();
                foreach (var level in _activePilgrims.Keys)
                {
                    CleanupDepartedPilgrims(level);
                    result[level] = _activePilgrims[level].Count;
                }
                return result;
            }
        }

        /// <summary>
        /// Calculates throughput (pilgrims per minute) for the specified level
        /// based on recent departures in the last 10 minutes.
        /// </summary>
        public double GetThroughputPerMinute(int level)
        {
            lock (_lock)
            {
                if (!_activePilgrims.ContainsKey(level))
                {
                    return 10.0; // Default throughput
                }

                var now = DateTime.UtcNow;
                var lookbackMinutes = 10.0;
                var lookbackTime = now.AddMinutes(-lookbackMinutes);

                // Count pilgrims who arrived in the lookback window
                // (they represent throughput as they're being processed)
                var recentArrivals = _activePilgrims[level]
                    .Count(p => p.ArrivalTime >= lookbackTime);

                // If we have data, calculate throughput, otherwise use default
                if (recentArrivals > 0)
                {
                    return recentArrivals / lookbackMinutes;
                }

                return 10.0; // Default throughput
            }
        }

        /// <summary>
        /// Calculates estimated wait time for the specified level.
        /// Wait time = Queue Length / Throughput
        /// </summary>
        public double GetEstimatedWaitMinutes(int level)
        {
            lock (_lock)
            {
                int queueLength = GetQueueLength(level);
                double throughput = GetThroughputPerMinute(level);

                if (throughput <= 0)
                {
                    return 12.0; // Default wait time
                }

                // Wait time = queue length / throughput
                return queueLength / throughput;
            }
        }

        /// <summary>
        /// Gets comprehensive statistics for all levels.
        /// </summary>
        public Dictionary<int, LevelStats> GetAllLevelStats()
        {
            lock (_lock)
            {
                var result = new Dictionary<int, LevelStats>();
                foreach (var level in _activePilgrims.Keys)
                {
                    CleanupDepartedPilgrims(level);
                    result[level] = new LevelStats
                    {
                        QueueLength = GetQueueLength(level),
                        ThroughputPerMinute = GetThroughputPerMinute(level),
                        EstimatedWaitMinutes = GetEstimatedWaitMinutes(level)
                    };
                }
                return result;
            }
        }

        /// <summary>
        /// Removes pilgrims who have departed (stayed longer than 45 minutes).
        /// </summary>
        private void CleanupDepartedPilgrims(int level)
        {
            if (!_activePilgrims.ContainsKey(level))
            {
                return;
            }

            var now = DateTime.UtcNow;
            _activePilgrims[level].RemoveAll(p => p.DepartureTime <= now);
        }

        private class PilgrimArrival
        {
            public DateTime ArrivalTime { get; set; }
            public DateTime DepartureTime { get; set; }
        }
    }

    public class LevelStats
    {
        public int QueueLength { get; set; }
        public double ThroughputPerMinute { get; set; }
        public double EstimatedWaitMinutes { get; set; }
    }
}
