# Load Balancer System - Simplified Version

## What Changed

The load balancer system has been **dramatically simplified** to match the actual problem:

### The Reality
- People walk into a level, perform a 45-minute ritual, and leave
- **There is NO waiting time**
- The only issue is crowding/cluttering
- Goal: Distribute people across 3 levels to minimize crowding

### What Was Wrong Before
The previous system was designed for a queueing problem with wait times, which doesn't match reality. It had:
- Wait time calculations
- Feedback controllers targeting wait times
- Dynamic age cutoffs based on statistical percentiles
- Rolling quantile estimators
- Throughput tracking
- 10+ configuration parameters

### What It Is Now
A simple occupancy-based distribution system:
- Tracks how many people are at each level
- Uses 3 simple rules to assign levels
- 2 configuration parameters
- ~150 lines of code

---

## Quick Start

### 1. Run the Application
```bash
dotnet run
```

### 2. Assign a Pilgrim to a Level
```bash
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 45, "isDisabled": false}'
```

### 3. Check Occupancy
```bash
curl http://localhost:5000/api/LoadBalancer/metrics
```

---

## Assignment Rules

1. **Disabled** → Always Level 1
2. **Age ≥ 60** → Prefer Level 1 (unless overcrowded, then redirect to 2/3)
3. **Age < 60** → Least crowded of Level 2 or 3

---

## Configuration

Only 2 parameters:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `ageThreshold` | 60 | Age at which pilgrims prefer Level 1 |
| `level1TargetShare` | 0.40 | Max share for Level 1 before redirecting elderly |

Update configuration:
```bash
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"ageThreshold": 65}'
```

---

## Files Changed

### Created/Modified
- ✅ `Services/LoadBalancerService.cs` - Completely rewritten (simplified)
- ✅ `Services/LoadBalancerConfig.cs` - Simplified to 2 parameters
- ✅ `Controllers/LoadBalancerController.cs` - Simplified endpoints
- ✅ `Models/ConfigUpdateRequest.cs` - Added new config fields

### Deleted
- 🗑️ `Services/AdaptiveController.cs`
- 🗑️ `Services/LevelStateManager.cs`
- 🗑️ `Services/RollingCounts.cs`
- 🗑️ `Services/RollingQuantileEstimator.cs`

### Kept (Still Useful)
- ✅ `Services/LevelTracker.cs` - Tracks 45-minute lifecycle
- ✅ All model classes (for backward compatibility)
- ✅ Legacy endpoints (for backward compatibility)

---

## Documentation

See `SIMPLE_LOAD_BALANCER.md` for full documentation.

---

## Backward Compatibility

All existing endpoints still work:
- `POST /api/LoadBalancer/arrivals/assign` ✅
- `GET /api/LoadBalancer/metrics` ✅
- `POST /api/LoadBalancer/config` ✅
- Legacy endpoints (no-op or simplified) ✅

Mobile apps and dashboards will continue to work without changes.

---

## Summary

**Problem:** Over-engineered solution for a simple problem

**Solution:** Stripped out all unnecessary complexity

**Result:** 10x simpler, easier to understand, and solves the actual problem

The system now matches what actually happens: people walk in, stay 45 minutes, leave. No waiting, just occupancy management.
