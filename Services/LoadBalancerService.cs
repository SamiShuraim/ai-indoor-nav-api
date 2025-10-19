using System.Collections.Concurrent;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    public class LoadBalancerService
    {
        // Level capacities
        private const int LEVEL_1_CAPACITY = 500;
        private const int LEVEL_2_CAPACITY = 3000;
        private const int LEVEL_3_CAPACITY = 3000;

        // Thresholds for determining when to start being selective
        private const double LEVEL_1_SELECTIVITY_THRESHOLD = 0.6; // 60% utilization
        private const int ELDERLY_AGE_THRESHOLD = 60; // Age 60+ considered elderly

        // Current utilization - using ConcurrentDictionary for thread safety
        private readonly ConcurrentDictionary<int, int> _levelUtilization = new()
        {
            [1] = 0,
            [2] = 0,
            [3] = 0
        };

        public LevelAssignmentResponse AssignLevel(LevelAssignmentRequest request)
        {
            int assignedLevel = DetermineLevel(request.Age, request.IsHealthy);
            
            // Increment utilization for the assigned level
            _levelUtilization.AddOrUpdate(assignedLevel, 1, (key, oldValue) => oldValue + 1);
            
            int currentUtil = _levelUtilization[assignedLevel];
            int capacity = GetCapacity(assignedLevel);
            
            return new LevelAssignmentResponse
            {
                AssignedLevel = assignedLevel,
                CurrentUtilization = currentUtil,
                Capacity = capacity,
                UtilizationPercentage = Math.Round((double)currentUtil / capacity * 100, 2)
            };
        }

        private int DetermineLevel(int age, bool isHealthy)
        {
            int level1Util = _levelUtilization[1];
            int level2Util = _levelUtilization[2];
            int level3Util = _levelUtilization[3];
            
            double level1UtilizationRate = (double)level1Util / LEVEL_1_CAPACITY;
            
            // Calculate priority score (higher score = higher priority for Level 1)
            // Factors: age (older = higher), health condition (unhealthy = higher)
            int priorityScore = CalculatePriorityScore(age, isHealthy);
            
            // If Level 1 is below selectivity threshold, assign everyone there
            if (level1UtilizationRate < LEVEL_1_SELECTIVITY_THRESHOLD)
            {
                // Still prefer Level 1 unless it's completely full
                if (level1Util < LEVEL_1_CAPACITY)
                {
                    return 1;
                }
            }
            
            // Level 1 is getting full, be selective
            // High priority users (elderly, unhealthy) get Level 1
            if (priorityScore >= 60) // Threshold for high priority
            {
                if (level1Util < LEVEL_1_CAPACITY)
                {
                    return 1;
                }
            }
            
            // Medium priority users can go to Level 1 if it's not too full
            if (priorityScore >= 40 && level1UtilizationRate < 0.8)
            {
                if (level1Util < LEVEL_1_CAPACITY)
                {
                    return 1;
                }
            }
            
            // For younger/healthy people, assign to Level 2 or 3
            // Balance between Level 2 and 3 to distribute load evenly
            if (level2Util <= level3Util && level2Util < LEVEL_2_CAPACITY)
            {
                return 2;
            }
            else if (level3Util < LEVEL_3_CAPACITY)
            {
                return 3;
            }
            else if (level2Util < LEVEL_2_CAPACITY)
            {
                return 2;
            }
            
            // If all levels are full, still assign to Level 1 for high priority
            // Otherwise, overflow to Level 2 or 3
            if (priorityScore >= 60)
            {
                return 1;
            }
            
            return level2Util <= level3Util ? 2 : 3;
        }

        private int CalculatePriorityScore(int age, bool isHealthy)
        {
            int score = 0;
            
            // Age component (0-70 points)
            // Linear scaling: 0 years = 0 points, 100+ years = 70 points
            score += Math.Min(70, (int)(age * 0.7));
            
            // Health component (0-30 points)
            // Unhealthy adds 30 points
            if (!isHealthy)
            {
                score += 30;
            }
            
            return score;
        }

        private int GetCapacity(int level)
        {
            return level switch
            {
                1 => LEVEL_1_CAPACITY,
                2 => LEVEL_2_CAPACITY,
                3 => LEVEL_3_CAPACITY,
                _ => throw new ArgumentException($"Invalid level: {level}")
            };
        }

        public LevelUtilizationResponse GetUtilization()
        {
            var response = new LevelUtilizationResponse();
            
            foreach (var level in new[] { 1, 2, 3 })
            {
                int util = _levelUtilization[level];
                int capacity = GetCapacity(level);
                
                response.Levels[level] = new LevelInfo
                {
                    Level = level,
                    CurrentUtilization = util,
                    Capacity = capacity,
                    UtilizationPercentage = Math.Round((double)util / capacity * 100, 2)
                };
            }
            
            return response;
        }

        // Optional: Method to reset utilization (useful for testing or daily resets)
        public void ResetUtilization()
        {
            _levelUtilization[1] = 0;
            _levelUtilization[2] = 0;
            _levelUtilization[3] = 0;
        }
    }
}
