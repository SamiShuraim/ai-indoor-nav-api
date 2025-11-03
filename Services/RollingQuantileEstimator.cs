using System.Collections.Concurrent;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Streaming quantile estimator using a sorted list approach for simplicity.
    /// For production, consider using t-digest or Greenwald-Khanna for better performance.
    /// </summary>
    public class RollingQuantileEstimator
    {
        private readonly object _lock = new object();
        private readonly List<TimestampedValue> _values = new();
        private readonly double _windowMinutes;
        private readonly bool _useDecay;
        private readonly double _halfLifeMinutes;

        public RollingQuantileEstimator(double windowMinutes, bool useDecay = false, double halfLifeMinutes = 45)
        {
            _windowMinutes = windowMinutes;
            _useDecay = useDecay;
            _halfLifeMinutes = halfLifeMinutes;
        }

        public void Insert(double age)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                _values.Add(new TimestampedValue { Value = age, Timestamp = now });
                CleanOldValues(now);
            }
        }

        public double GetQuantile(double p)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                CleanOldValues(now);

                if (_values.Count == 0)
                {
                    return double.NegativeInfinity;
                }

                if (_useDecay)
                {
                    return GetQuantileWithDecay(p, now);
                }
                else
                {
                    return GetQuantileSimple(p);
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _values.Count;
                }
            }
        }

        private double GetQuantileSimple(double p)
        {
            var sorted = _values.Select(v => v.Value).OrderBy(v => v).ToList();
            int index = (int)Math.Ceiling(p * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(sorted.Count - 1, index));
            return sorted[index];
        }

        private double GetQuantileWithDecay(double p, DateTime now)
        {
            // Calculate weights using exponential decay
            var weightedValues = _values.Select(v =>
            {
                double ageMinutes = (now - v.Timestamp).TotalMinutes;
                double weight = Math.Exp(-Math.Log(2) * ageMinutes / _halfLifeMinutes);
                return new { v.Value, Weight = weight };
            }).OrderBy(v => v.Value).ToList();

            double totalWeight = weightedValues.Sum(v => v.Weight);
            if (totalWeight == 0) return double.NegativeInfinity;

            double targetWeight = p * totalWeight;
            double cumWeight = 0;

            foreach (var item in weightedValues)
            {
                cumWeight += item.Weight;
                if (cumWeight >= targetWeight)
                {
                    return item.Value;
                }
            }

            return weightedValues.Last().Value;
        }

        private void CleanOldValues(DateTime now)
        {
            if (!_useDecay)
            {
                // Remove values older than the window
                _values.RemoveAll(v => (now - v.Timestamp).TotalMinutes > _windowMinutes);
            }
            // For decay mode, we keep all values but weight them
        }

        private class TimestampedValue
        {
            public double Value { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}
