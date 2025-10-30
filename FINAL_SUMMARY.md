# Load Balancer - Final Clean System

## Summary

All legacy code and documentation has been removed. The system now only contains the **quantile-based occupancy load balancer** with dynamic age cutoffs.

---

## What Was Deleted

### Documentation (11 files)
- ❌ `ADAPTIVE_LOAD_BALANCER.md`
- ❌ `ADAPTIVE_LOAD_BALANCER_QUICK_START.md`
- ❌ `LOAD_BALANCER_IMPLEMENTATION.md`
- ❌ `LOAD_BALANCER_QUICK_START.md`
- ❌ `WAIT_TIME_EXPLAINED.md`
- ❌ `SIMPLE_OCCUPANCY_SYSTEM.md`
- ❌ `SIMPLE_LOAD_BALANCER.md`
- ❌ `README_SIMPLIFIED_SYSTEM.md`
- ❌ `CHANGELOG_SIMPLIFIED.md`
- ❌ `AUTOMATIC_LEVEL_TRACKING.md`
- ❌ `SYSTEM_EXPLANATION_SIMPLE.md`
- ❌ `MOBILE_AND_DASHBOARD_INTEGRATION.md`

### Model Classes (5 files)
- ❌ `Models/LevelAssignmentRequest.cs`
- ❌ `Models/LevelAssignmentResponse.cs`
- ❌ `Models/LevelUtilizationResponse.cs`
- ❌ `Models/ControlTickResponse.cs`
- ❌ `Models/LevelStateUpdateRequest.cs`

### Test Scripts (1 file)
- ❌ `test-adaptive-load-balancer.sh`

### Legacy Code Removed
- ❌ All legacy endpoint methods in `LoadBalancerController`
- ❌ All legacy support methods in `LoadBalancerService`
- ❌ Legacy fields in config models

---

## What Remains

### Core Service Files
- ✅ `Services/LoadBalancerService.cs` - Clean quantile-based implementation
- ✅ `Services/LoadBalancerConfig.cs` - Simple config (alpha1, window params)
- ✅ `Services/LevelTracker.cs` - Tracks 45-minute occupancy lifecycle
- ✅ `Services/RollingQuantileEstimator.cs` - Computes percentiles
- ✅ `Services/RollingCounts.cs` - Tracks disabled fraction

### Controllers & Models
- ✅ `Controllers/LoadBalancerController.cs` - Clean API endpoints
- ✅ `Models/ArrivalAssignRequest.cs`
- ✅ `Models/ArrivalAssignResponse.cs` - Now uses `Occupancy` (not `WaitEst`)
- ✅ `Models/MetricsResponse.cs` - Cleaned up, removed legacy fields
- ✅ `Models/ConfigUpdateRequest.cs` - Only relevant fields

### Documentation (4 files)
- ✅ `QUANTILE_BASED_LOAD_BALANCER.md` - Complete algorithm documentation
- ✅ `README_QUANTILE_SYSTEM.md` - Quick start guide
- ✅ `SYSTEM_SUMMARY.md` - System overview
- ✅ `CHANGELOG_QUANTILE_SYSTEM.md` - What changed and why

---

## Current API Endpoints

### Active Endpoints

1. **POST** `/api/LoadBalancer/arrivals/assign`
   - Assign pilgrim to a level
   - Returns: level, decision info, occupancy

2. **GET** `/api/LoadBalancer/metrics`
   - Get current metrics
   - Returns: alpha1, ageCutoff, pDisabled, occupancy, quantiles

3. **POST** `/api/LoadBalancer/config`
   - Update configuration
   - Parameters: alpha1, alpha1Min, alpha1Max, window

4. **GET** `/api/LoadBalancer/config`
   - Get current configuration

5. **GET** `/api/LoadBalancer/health`
   - Health check

### Removed Endpoints
- ❌ `POST /api/LoadBalancer/levels/state` - No longer needed
- ❌ `POST /api/LoadBalancer/control/tick` - No controller
- ❌ `POST /api/LoadBalancer/assign` - Legacy format
- ❌ `GET /api/LoadBalancer/utilization` - Use metrics instead
- ❌ `POST /api/LoadBalancer/reset` - Not applicable

---

## Response Format Changes

### Before (with legacy fields)
```json
{
  "level": 1,
  "decision": {
    "waitEst": { "1": 45, "2": 38, "3": 52 }
  }
}
```

### After (clean)
```json
{
  "level": 1,
  "decision": {
    "occupancy": { "1": 45, "2": 38, "3": 52 }
  }
}
```

### Metrics Before
```json
{
  "waitTargetMinutes": 0,
  "controllerGain": 0,
  "levels": {
    "1": {
      "waitEst": 45,
      "queueLength": 45,
      "throughputPerMin": 0
    }
  }
}
```

### Metrics After
```json
{
  "levels": {
    "1": {
      "occupancy": 45
    }
  }
}
```

---

## Configuration Changes

### Before (many legacy fields)
```json
{
  "ageThreshold": 0,
  "level1TargetShare": 0.35,
  "alpha1": 0.35,
  "waitTargetMinutes": 0,
  "controllerGain": 0,
  "softGate": null,
  "randomization": null
}
```

### After (clean)
```json
{
  "alpha1": 0.35,
  "alpha1Min": 0.15,
  "alpha1Max": 0.55,
  "window": {
    "mode": "sliding",
    "minutes": 45,
    "halfLifeMinutes": 45
  }
}
```

---

## The System Now

### What It Does
1. **Tracks recent arrivals** (rolling 45-minute window)
2. **Computes dynamic age cutoff** using quantiles
3. **Assigns pilgrims** based on age vs cutoff and current occupancy
4. **NO wait times** - just occupancy management

### Key Algorithm

```
Given:
- alpha1 = 0.35 (want 35% to Level 1)
- p_disabled = 0.15 (measured: 15% disabled)

Compute:
- share_left_for_old = 0.35 - 0.15 = 0.20
- tau = 1 - 0.20 = 0.80
- age_cutoff = 80th percentile of recent non-disabled ages

Assign:
- IF disabled → Level 1
- ELSE IF age >= age_cutoff → Level 1 (unless overcrowded)
- ELSE → Least crowded of Level 2/3
```

### Configuration

**One main parameter:** `alpha1` (default: 0.35)
- Controls target share for Level 1
- Higher → More to Level 1 → Lower age cutoff
- Lower → Fewer to Level 1 → Higher age cutoff

---

## Quick Start

### Run the Application
```bash
dotnet run
```

### Assign a Pilgrim
```bash
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 65, "isDisabled": false}'
```

### Check Metrics
```bash
curl http://localhost:5000/api/LoadBalancer/metrics
```

### Update Config
```bash
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"alpha1": 0.40}'
```

---

## File Count Summary

**Before cleanup:**
- 19 markdown files (many legacy)
- 18 model files
- Multiple legacy endpoints and methods

**After cleanup:**
- 4 markdown files (all current)
- 13 model files (all relevant)
- Clean API with 5 endpoints

---

## Breaking Changes

### ⚠️ API Changes

1. **Response field renamed:**
   - `decision.waitEst` → `decision.occupancy`
   - Type changed: `Dictionary<int, double>` → `Dictionary<int, int>`

2. **Metrics response cleaned:**
   - Removed: `waitTargetMinutes`, `controllerGain`
   - Changed: `levels[x].waitEst` → `levels[x].occupancy`
   - Removed: `queueLength`, `throughputPerMin`

3. **Config response cleaned:**
   - Removed: `ageThreshold`, `level1TargetShare`, `waitTargetMinutes`, `controllerGain`, `softGate`, `randomization`

4. **Endpoints removed:**
   - `POST /api/LoadBalancer/levels/state`
   - `POST /api/LoadBalancer/control/tick`
   - `POST /api/LoadBalancer/assign`
   - `GET /api/LoadBalancer/utilization`
   - `POST /api/LoadBalancer/reset`

### Migration

**For mobile apps:**
- Change `decision.waitEst` → `decision.occupancy`
- Update to integer type

**For dashboards:**
- Use `GET /api/LoadBalancer/metrics` instead of `/utilization`
- Display `occupancy` instead of `waitEst`

---

## Documentation

See these files for details:
- `QUANTILE_BASED_LOAD_BALANCER.md` - Complete algorithm and API docs
- `README_QUANTILE_SYSTEM.md` - Quick start guide
- `SYSTEM_SUMMARY.md` - System overview

---

## Result

**Clean, focused system:**
- No legacy code
- No wait time calculations
- No feedback controllers
- Just quantile-based dynamic age cutoffs with occupancy tracking

**Simple and correct:**
- Adapts to any age distribution
- No fixed thresholds
- Solves the actual problem (occupancy distribution)
