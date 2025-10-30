# Load Balancer System - Current Implementation

## What It Is

A **quantile-based occupancy load balancer** that uses dynamic age cutoffs computed from the distribution of recent arrivals to distribute pilgrims across 3 levels and minimize crowding.

---

## Core Concepts

### ✅ What It Does

1. **Dynamic Age Cutoffs** - Age threshold is NOT fixed (like "60+"), but computed from quantiles of recent arrivals
2. **Occupancy Tracking** - Tracks how many people are currently at each level (they stay 45 minutes)
3. **Distribution-Based** - Uses statistical percentiles to adapt to the actual population

### ❌ What It Doesn't Do

1. **NO Wait Times** - People don't wait, they just walk in
2. **NO Queue Calculations** - No queuing, just occupancy
3. **NO Feedback Controllers** - No automatic adjustment of parameters based on wait times

---

## The Algorithm

### Step 1: Track Arrivals (Rolling Window)
- Keep last 45 minutes of arrivals
- Track ages of non-disabled pilgrims
- Track disabled fraction (`p_disabled`)

### Step 2: Compute Dynamic Age Cutoff

```
alpha1 = 0.35  // Target: 35% of arrivals go to Level 1 (configurable)
p_disabled = 0.15  // 15% of arrivals are disabled (measured)
share_left_for_old = 0.35 - 0.15 = 0.20  // 20% left for elderly
tau = 1 - 0.20 = 0.80  // 80th percentile
age_cutoff = 80th percentile of recent non-disabled ages
```

**Example:**
- Recent ages: [25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75, 80]
- 80th percentile ≈ 68
- Age cutoff = 68

### Step 3: Assign Based on Cutoff

```python
if disabled:
    return Level 1
elif age >= age_cutoff:
    if Level 1 severely overcrowded:
        return least_crowded(Level 2, Level 3)
    else:
        return Level 1
else:
    return least_crowded(Level 2, Level 3)
```

---

## Why This Works

### Adapts to Population

**Young crowd (ages 20-45):**
- 80th percentile might be 40
- Only 40+ go to Level 1
- ✅ Achieves ~35% distribution

**Old crowd (ages 60-90):**
- 80th percentile might be 78
- Only 78+ go to Level 1
- ✅ Achieves ~35% distribution

**Mixed crowd (ages 20-90):**
- 80th percentile might be 65
- 65+ go to Level 1
- ✅ Achieves ~35% distribution

### Handles Variable Disabled Fraction

**More disabled (p_disabled = 0.30):**
- share_left_for_old = 0.35 - 0.30 = 0.05
- tau = 0.95 (95th percentile)
- Age cutoff RISES (fewer elderly fit)

**Fewer disabled (p_disabled = 0.05):**
- share_left_for_old = 0.35 - 0.05 = 0.30
- tau = 0.70 (70th percentile)
- Age cutoff DROPS (more elderly fit)

---

## Configuration

### Main Parameter: `alpha1`

**Default: 0.35 (35% to Level 1)**

- Higher (e.g., 0.45) → Lower age cutoff → More people to Level 1
- Lower (e.g., 0.25) → Higher age cutoff → Fewer people to Level 1

```bash
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"alpha1": 0.40}'
```

### Other Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `alpha1` | 0.35 | Target share for Level 1 |
| `alpha1Min` | 0.15 | Minimum alpha1 |
| `alpha1Max` | 0.55 | Maximum alpha1 |
| `slidingWindowMinutes` | 45 | Rolling window duration |
| `windowMode` | "sliding" | "sliding" or "decay" |

---

## API Examples

### Assign a Pilgrim

```bash
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 65, "isDisabled": false}'
```

Response:
```json
{
  "level": 1,
  "decision": {
    "age": 65,
    "ageCutoff": 62.3,
    "alpha1": 0.35,
    "pDisabled": 0.15,
    "shareLeftForOld": 0.20,
    "tauQuantile": 0.80,
    "waitEst": {
      "1": 45,  // Occupancy (not wait time!)
      "2": 38,
      "3": 52
    },
    "reason": "Age 65 >= dynamic cutoff (62.3)"
  }
}
```

### Get Metrics

```bash
curl http://localhost:5000/api/LoadBalancer/metrics
```

Response:
```json
{
  "alpha1": 0.35,
  "ageCutoff": 62.3,
  "pDisabled": 0.15,
  "counts": {
    "total": 230,
    "disabled": 35,
    "nonDisabled": 195
  },
  "quantilesNonDisabledAge": {
    "q50": 44.2,
    "q80": 62.3,
    "q90": 71.5
  },
  "levels": {
    "1": { "queueLength": 45 },
    "2": { "queueLength": 38 },
    "3": { "queueLength": 52 }
  }
}
```

---

## Files Overview

### Core Service Files
- `Services/LoadBalancerService.cs` - Main service with quantile-based logic
- `Services/LoadBalancerConfig.cs` - Configuration (alpha1, window params)
- `Services/LevelTracker.cs` - Tracks 45-minute occupancy lifecycle
- `Services/RollingQuantileEstimator.cs` - Computes percentiles from recent ages
- `Services/RollingCounts.cs` - Tracks disabled fraction

### Controllers & Models
- `Controllers/LoadBalancerController.cs` - API endpoints
- `Models/` - Request/response models

### Documentation
- `QUANTILE_BASED_LOAD_BALANCER.md` - Complete algorithm documentation
- `README_QUANTILE_SYSTEM.md` - Quick start guide
- `SYSTEM_SUMMARY.md` - This file

---

## Key Insights

### Why Not Fixed Threshold (e.g., "age >= 60")?

**Problem with fixed:**
- If crowd is mostly 70+, everyone goes to Level 1 (overcrowding)
- If crowd is mostly 30-40, almost no one goes to Level 1 (underutilized)

**Solution with quantiles:**
- Always achieves target distribution (e.g., 35%)
- Works with any age distribution
- Adapts automatically

### Why Not Wait Times?

**Reality:**
- People walk in → Perform 45-min ritual → Leave
- NO waiting, NO queuing
- Just occupancy management

**What we track:**
- How many people are currently at each level
- That's it!

---

## Testing

### Verify Dynamic Cutoff Works

```bash
# Send young arrivals
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
    -H "Content-Type: application/json" \
    -d "{\"age\": $((20 + RANDOM % 25)), \"isDisabled\": false}" -s
done

# Check cutoff (should be low)
curl -s http://localhost:5000/api/LoadBalancer/metrics | jq '.ageCutoff'

# Send older arrivals
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
    -H "Content-Type: application/json" \
    -d "{\"age\": $((60 + RANDOM % 25)), \"isDisabled\": false}" -s
done

# Check cutoff (should have risen)
curl -s http://localhost:5000/api/LoadBalancer/metrics | jq '.ageCutoff'
```

### Verify Disabled Handling

```bash
# Disabled always get Level 1
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 25, "isDisabled": true}' | jq '.level'
# Should return: 1
```

---

## Comparison: Before vs After

### Original Complex System
- ❌ Wait time calculations
- ❌ Feedback controllers adjusting alpha1 based on wait times
- ❌ Soft gates and randomization
- ✅ Dynamic age cutoffs (kept!)
- ✅ Quantile estimation (kept!)

### Current Simplified System
- ✅ Dynamic age cutoffs based on quantiles
- ✅ Occupancy tracking
- ✅ User-configured alpha1 (not auto-adjusted)
- ❌ NO wait time calculations
- ❌ NO feedback controllers

---

## Summary

**What:** Quantile-based occupancy load balancer

**How:** Compute dynamic age cutoff from percentiles of recent arrivals

**Why:** Adapts to any age distribution while achieving target Level 1 share

**Result:** Simple, adaptive, and solves the actual problem (occupancy distribution, not wait times)
