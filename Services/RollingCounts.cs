namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Tracks rolling counts of arrivals (total, disabled, non-disabled).
    /// Supports both sliding window and exponential decay modes.
    /// </summary>
    public class RollingCounts
    {
        private readonly object _lock = new object();
        private readonly List<TimestampedArrival> _arrivals = new();
        private readonly double _windowMinutes;
        private readonly bool _useDecay;
        private readonly double _halfLifeMinutes;

        public RollingCounts(double windowMinutes, bool useDecay = false, double halfLifeMinutes = 45)
        {
            _windowMinutes = windowMinutes;
            _useDecay = useDecay;
            _halfLifeMinutes = halfLifeMinutes;
        }

        public void RecordArrival(bool isDisabled)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                _arrivals.Add(new TimestampedArrival { IsDisabled = isDisabled, Timestamp = now });
                CleanOldArrivals(now);
            }
        }

        public (int total, int disabled, int nonDisabled) GetCounts()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                CleanOldArrivals(now);

                if (_useDecay)
                {
                    return GetCountsWithDecay(now);
                }
                else
                {
                    int total = _arrivals.Count;
                    int disabled = _arrivals.Count(a => a.IsDisabled);
                    int nonDisabled = total - disabled;
                    return (total, disabled, nonDisabled);
                }
            }
        }

        public double GetPDisabled()
        {
            var (total, disabled, _) = GetCounts();
            return total > 0 ? (double)disabled / total : 0.0;
        }

        private (int total, int disabled, int nonDisabled) GetCountsWithDecay(DateTime now)
        {
            double totalWeight = 0;
            double disabledWeight = 0;

            foreach (var arrival in _arrivals)
            {
                double ageMinutes = (now - arrival.Timestamp).TotalMinutes;
                double weight = Math.Exp(-Math.Log(2) * ageMinutes / _halfLifeMinutes);
                
                totalWeight += weight;
                if (arrival.IsDisabled)
                {
                    disabledWeight += weight;
                }
            }

            // Round to nearest integer for counts
            int total = (int)Math.Round(totalWeight);
            int disabled = (int)Math.Round(disabledWeight);
            int nonDisabled = total - disabled;

            return (total, disabled, nonDisabled);
        }

        private void CleanOldArrivals(DateTime now)
        {
            if (!_useDecay)
            {
                _arrivals.RemoveAll(a => (now - a.Timestamp).TotalMinutes > _windowMinutes);
            }
        }

        private class TimestampedArrival
        {
            public bool IsDisabled { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
