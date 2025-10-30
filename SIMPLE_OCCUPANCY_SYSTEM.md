# Simple Occupancy-Based Load Balancer

## The Actual Problem

**Reality:** 
- 3 levels (floors/areas) for performing rituals
- Everyone spends 45 minutes then leaves
- NO waiting in queues - people just walk in
- Problem: Avoid overcrowding any single level
- Preference: Disabled and older people should go to Level 1 (easier access)

**NOT a queueing problem - it's a CAPACITY/OCCUPANCY problem!**

---

## What We Actually Need to Track

### Only 2 Things:

1. **Current Occupancy** = How many people are currently at each level
2. **Capacity Limits** = Maximum comfortable capacity for each level (optional)

That's it!

---

## Simple Assignment Algorithm

### Version 1: Pure Occupancy Balancing

```csharp
public int AssignLevel(int age, bool isDisabled)
{
    // Get current occupancy
    int level1Count = GetOccupancy(1);  // e.g., 45 people
    int level2Count = GetOccupancy(2);  // e.g., 52 people
    int level3Count = GetOccupancy(3);  // e.g., 48 people
    
    // Rule 1: Disabled always to Level 1
    if (isDisabled)
        return 1;
    
    // Rule 2: Older people (age >= 60) prefer Level 1, but avoid overcrowding
    if (age >= 60)
    {
        // If Level 1 isn't too crowded, send them there
        if (level1Count <= Math.Min(level2Count, level3Count) + 10)
            return 1;
        
        // Otherwise, send to least crowded of 2/3
        return level2Count <= level3Count ? 2 : 3;
    }
    
    // Rule 3: Younger people go to least crowded of Level 2 or 3
    return level2Count <= level3Count ? 2 : 3;
}
```

**That's the entire algorithm!** No feedback controllers, no statistics, no wait times.

---

### Version 2: With Capacity Limits

If you want to enforce maximum capacity:

```csharp
public int AssignLevel(int age, bool isDisabled)
{
    int level1Count = GetOccupancy(1);
    int level2Count = GetOccupancy(2);
    int level3Count = GetOccupancy(3);
    
    const int MAX_CAPACITY = 80;  // Max people per level
    
    // Rule 1: Disabled to Level 1 (even if over capacity)
    if (isDisabled)
        return 1;
    
    // Rule 2: Older people prefer Level 1
    if (age >= 60)
    {
        if (level1Count < MAX_CAPACITY)
            return 1;
        
        // Level 1 full, redirect to least crowded
        return level2Count <= level3Count ? 2 : 3;
    }
    
    // Rule 3: Younger people to least crowded
    return level2Count <= level3Count ? 2 : 3;
}
```

---

### Version 3: Weighted Distribution (Most Sophisticated)

If you want Level 1 to handle about 35% of traffic:

```csharp
public int AssignLevel(int age, bool isDisabled)
{
    int level1Count = GetOccupancy(1);
    int level2Count = GetOccupancy(2);
    int level3Count = GetOccupancy(3);
    int totalCount = level1Count + level2Count + level3Count;
    
    // Rule 1: Disabled to Level 1
    if (isDisabled)
        return 1;
    
    // Rule 2: Older people prefer Level 1
    if (age >= 60)
    {
        // Check if Level 1 is under its target share (35%)
        if (totalCount == 0 || level1Count < totalCount * 0.40)
            return 1;
        
        // Level 1 over capacity, redirect
        return level2Count <= level3Count ? 2 : 3;
    }
    
    // Rule 3: Younger people to least crowded
    return level2Count <= level3Count ? 2 : 3;
}
```

---

## Implementation

### Replace LoadBalancerService.AssignArrival()

The current implementation has:
- ❌ Wait time calculations
- ❌ Throughput tracking
- ❌ Feedback controllers
- ❌ Quantile estimators
- ❌ Rolling statistics
- ❌ Soft gates and randomization

**You only need:**
- ✅ Track occupancy (people currently at each level)
- ✅ Simple assignment rules

---

## New Simplified Service

```csharp
public class SimpleLoadBalancerService
{
    private readonly LevelTracker _levelTracker;
    private const int AGE_THRESHOLD = 60;
    private const double LEVEL1_TARGET_SHARE = 0.35;
    
    public SimpleLoadBalancerService()
    {
        _levelTracker = new LevelTracker(); // Keep this - it tracks 45-min lifecycle
    }
    
    public ArrivalAssignResponse AssignArrival(ArrivalAssignRequest request)
    {
        // Validate age
        if (request.Age < 0 || request.Age > 120)
            throw new ArgumentException($"Age must be between 0 and 120");
        
        // Get current occupancy
        var occupancy = _levelTracker.GetAllQueueLengths();
        int level1 = occupancy.GetValueOrDefault(1, 0);
        int level2 = occupancy.GetValueOrDefault(2, 0);
        int level3 = occupancy.GetValueOrDefault(3, 0);
        int total = level1 + level2 + level3;
        
        // Assign level
        int assignedLevel;
        string reason;
        
        if (request.IsDisabled)
        {
            assignedLevel = 1;
            reason = "Disabled pilgrims assigned to Level 1";
        }
        else if (request.Age >= AGE_THRESHOLD)
        {
            // Prefer Level 1 but don't overload it
            if (total == 0 || level1 < total * 0.40)
            {
                assignedLevel = 1;
                reason = $"Age {request.Age} >= {AGE_THRESHOLD}, Level 1 under capacity";
            }
            else
            {
                assignedLevel = level2 <= level3 ? 2 : 3;
                reason = $"Age {request.Age} >= {AGE_THRESHOLD} but Level 1 over capacity, redirected";
            }
        }
        else
        {
            assignedLevel = level2 <= level3 ? 2 : 3;
            reason = $"Age {request.Age} < {AGE_THRESHOLD}, assigned to least crowded";
        }
        
        // Record arrival
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
                Reason = reason,
                WaitEst = new Dictionary<int, double>
                {
                    [1] = occupancy.GetValueOrDefault(1, 0),
                    [2] = occupancy.GetValueOrDefault(2, 0),
                    [3] = occupancy.GetValueOrDefault(3, 0)
                }
            },
            TraceId = Guid.NewGuid().ToString()
        };
    }
    
    public OccupancyMetrics GetMetrics()
    {
        var occupancy = _levelTracker.GetAllQueueLengths();
        
        return new OccupancyMetrics
        {
            Levels = occupancy.ToDictionary(
                kvp => kvp.Key,
                kvp => new LevelOccupancy { CurrentOccupancy = kvp.Value }
            )
        };
    }
}
```

---

## What Gets Removed

You can DELETE or IGNORE:
- ❌ `AdaptiveController` - Not needed
- ❌ `RollingQuantileEstimator` - Not needed
- ❌ `RollingCounts` - Not needed
- ❌ `LoadBalancerConfig` (most of it) - Just need age threshold
- ❌ `LevelStateManager` - Not needed
- ❌ Controller tick logic - Not needed
- ❌ Wait time calculations - Not needed
- ❌ Throughput calculations - Not needed

You KEEP:
- ✅ `LevelTracker` - Tracks 45-minute lifecycle (this is useful!)
- ✅ Basic assignment logic
- ✅ Occupancy tracking

---

## Mobile App Impact

**No change!** Still just:
```json
POST /api/LoadBalancer/arrivals/assign
{
  "age": 45,
  "isDisabled": false
}

Response:
{
  "level": 2,
  "decision": {
    "reason": "Age 45 < 60, assigned to least crowded",
    "waitEst": {  // Now these are OCCUPANCY COUNTS, not wait times!
      "1": 45,     // 45 people currently at Level 1
      "2": 38,     // 38 people currently at Level 2
      "3": 52      // 52 people currently at Level 3
    }
  }
}
```

---

## Admin Dashboard Impact

**Simplified metrics:**
```json
GET /api/LoadBalancer/metrics

{
  "levels": {
    "1": { "currentOccupancy": 45 },
    "2": { "currentOccupancy": 38 },
    "3": { "currentOccupancy": 52 }
  }
}
```

No more alpha1, ageCutoff, pDisabled, quantiles, etc. Just **how many people are at each level**.

---

## Summary

**OLD SYSTEM (Wrong):**
- Complex feedback controller
- Wait time targeting (irrelevant!)
- Dynamic age cutoffs
- 10+ configuration parameters
- Hundreds of lines of complex logic

**NEW SYSTEM (Right):**
- Track occupancy at each level
- Simple rules: disabled → 1, old → prefer 1, young → balance 2/3
- Maybe 50 lines of code
- Easy to understand and predict

---

## Do You Want Me To Implement This?

I can:
1. Create the simplified service
2. Update the controller
3. Remove all the unnecessary complexity
4. Update the test script
5. Update documentation

This will be **10x simpler** and actually solve your real problem!

Should I do it?
