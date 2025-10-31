namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Tracks active assignments with automatic expiry based on dwell time.
    /// </summary>
    public class AssignmentLog
    {
        private readonly object _lock = new object();
        private readonly List<Assignment> _assignments = new();
        private readonly double _dwellMinutes;
        private readonly double _ttlBufferMinutes;

        public AssignmentLog(double dwellMinutes, double ttlBufferMinutes)
        {
            _dwellMinutes = dwellMinutes;
            _ttlBufferMinutes = ttlBufferMinutes;
        }

        /// <summary>
        /// Adds a new assignment.
        /// </summary>
        public void Add(int level, int age, bool isDisabled, DateTime timestamp)
        {
            lock (_lock)
            {
                _assignments.Add(new Assignment
                {
                    Level = level,
                    Age = age,
                    IsDisabled = isDisabled,
                    Timestamp = timestamp
                });
            }
        }

        /// <summary>
        /// Removes expired assignments (older than dwell_minutes + ttl_buffer_minutes).
        /// </summary>
        public void EvictExpired(DateTime now)
        {
            lock (_lock)
            {
                double expiryMinutes = _dwellMinutes + _ttlBufferMinutes;
                _assignments.RemoveAll(a => (now - a.Timestamp).TotalMinutes > expiryMinutes);
            }
        }

        /// <summary>
        /// Gets count of active assignments per level.
        /// </summary>
        public Dictionary<int, int> GetActiveCounts()
        {
            lock (_lock)
            {
                var counts = new Dictionary<int, int>
                {
                    [1] = 0,
                    [2] = 0,
                    [3] = 0
                };

                foreach (var assignment in _assignments)
                {
                    if (counts.ContainsKey(assignment.Level))
                    {
                        counts[assignment.Level]++;
                    }
                }

                return counts;
            }
        }

        /// <summary>
        /// Gets assignments within a time window for computing recent share.
        /// </summary>
        public List<Assignment> GetRecentAssignments(DateTime now, double windowMinutes)
        {
            lock (_lock)
            {
                var cutoff = now.AddMinutes(-windowMinutes);
                return _assignments.Where(a => a.Timestamp >= cutoff).ToList();
            }
        }

        /// <summary>
        /// Gets total count of active assignments.
        /// </summary>
        public int GetTotalActive()
        {
            lock (_lock)
            {
                return _assignments.Count;
            }
        }

        private class Assignment
        {
            public int Level { get; set; }
            public int Age { get; set; }
            public bool IsDisabled { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }

    public class RateLimiter
    {
        private readonly object _lock = new object();
        private int _currentMinute = -1;
        private int _countInCurrentMinute = 0;

        /// <summary>
        /// Attempts to admit to L1. Returns true if admitted, false if rate limit exceeded.
        /// </summary>
        public bool TryAdmit(DateTime now, int rateLimit)
        {
            lock (_lock)
            {
                int minute = now.Minute + now.Hour * 60 + now.Day * 24 * 60;
                
                // Reset counter on minute boundary
                if (minute != _currentMinute)
                {
                    _currentMinute = minute;
                    _countInCurrentMinute = 0;
                }

                if (_countInCurrentMinute >= rateLimit)
                {
                    return false; // Rate limit exceeded
                }

                _countInCurrentMinute++;
                return true;
            }
        }

        /// <summary>
        /// Gets remaining capacity in current minute.
        /// </summary>
        public int GetRemaining(DateTime now, int rateLimit)
        {
            lock (_lock)
            {
                int minute = now.Minute + now.Hour * 60 + now.Day * 24 * 60;
                
                if (minute != _currentMinute)
                {
                    return rateLimit; // Fresh minute
                }

                return Math.Max(0, rateLimit - _countInCurrentMinute);
            }
        }
    }
}
