using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Service to track and manage visitor information
    /// </summary>
    public class VisitorService
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, Visitor> _visitors = new();

        /// <summary>
        /// Creates a new visitor record and returns a unique ID
        /// </summary>
        public string CreateVisitor(int age, bool isDisabled, int assignedLevel, DateTime assignedAt, double dwellMinutes)
        {
            lock (_lock)
            {
                // Generate a short, scannable visitor ID (8 characters)
                string visitorId = GenerateVisitorId();
                
                var visitor = new Visitor
                {
                    VisitorId = visitorId,
                    Age = age,
                    IsDisabled = isDisabled,
                    AssignedLevel = assignedLevel,
                    AssignedAt = assignedAt,
                    ExpiresAt = assignedAt.AddMinutes(dwellMinutes)
                };

                _visitors[visitorId] = visitor;

                // Clean up old expired visitors periodically
                CleanupExpiredVisitors();

                return visitorId;
            }
        }

        /// <summary>
        /// Retrieves visitor information by ID
        /// </summary>
        public Visitor? GetVisitor(string visitorId)
        {
            lock (_lock)
            {
                if (_visitors.TryGetValue(visitorId, out var visitor))
                {
                    return visitor;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets visitor info formatted for response
        /// </summary>
        public VisitorInfoResponse? GetVisitorInfo(string visitorId)
        {
            var visitor = GetVisitor(visitorId);
            if (visitor == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            return new VisitorInfoResponse
            {
                VisitorId = visitor.VisitorId,
                Age = visitor.Age,
                Status = visitor.IsDisabled ? "Disabled" : "Non-Disabled",
                AssignedLevel = visitor.AssignedLevel,
                AssignedAt = visitor.AssignedAt,
                ExpiresAt = visitor.ExpiresAt,
                IsExpired = now > visitor.ExpiresAt
            };
        }

        /// <summary>
        /// Generates a short, scannable visitor ID
        /// </summary>
        private string GenerateVisitorId()
        {
            // Generate 8-character alphanumeric ID (uppercase for readability)
            // Format: XXXX-XXXX for easy scanning
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude similar chars (I/1, O/0)
            var random = new Random();
            
            string id;
            do
            {
                var part1 = new string(Enumerable.Range(0, 4)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
                var part2 = new string(Enumerable.Range(0, 4)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
                
                id = $"{part1}-{part2}";
            }
            while (_visitors.ContainsKey(id)); // Ensure uniqueness

            return id;
        }

        /// <summary>
        /// Removes visitors that expired more than 1 hour ago
        /// </summary>
        private void CleanupExpiredVisitors()
        {
            var now = DateTime.UtcNow;
            var cutoff = now.AddHours(-1); // Keep records for 1 hour after expiry
            
            var expiredIds = _visitors
                .Where(kvp => kvp.Value.ExpiresAt < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var id in expiredIds)
            {
                _visitors.Remove(id);
            }
        }

        /// <summary>
        /// Gets total count of active visitors
        /// </summary>
        public int GetActiveVisitorCount()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                return _visitors.Values.Count(v => v.ExpiresAt > now);
            }
        }
    }
}
