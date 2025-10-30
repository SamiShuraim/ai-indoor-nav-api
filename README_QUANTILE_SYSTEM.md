# Quantile-Based Load Balancer - Quick Start

## What Is This?

A load balancer that uses **dynamic age cutoffs** computed from the actual distribution of arriving pilgrims.

### Key Features
- ✅ **No fixed age threshold** - Cutoff adapts to the crowd
- ✅ **Distribution-based** - Uses quantiles (percentiles) of recent arrivals
- ✅ **Occupancy tracking** - No wait times, just track how many people are at each level
- ✅ **Handles varying populations** - Works with young crowds, old crowds, mixed crowds

---

## Quick Example

### Scenario 1: Young Crowd
```
Recent arrivals: ages 20, 25, 30, 35, 40, 45, 50, 55, 60, 65
Target: 35% to Level 1
Disabled: 10%
→ Age cutoff: ~56 (80th percentile)
→ Ages 56+ go to Level 1
```

### Scenario 2: Older Crowd
```
Recent arrivals: ages 50, 55, 60, 65, 70, 75, 80, 85, 90, 95
Target: 35% to Level 1
Disabled: 10%
→ Age cutoff: ~86 (80th percentile)
→ Ages 86+ go to Level 1
```

**The cutoff adapts to the population!**

---

## How It Works (Simple Version)

1. **Track recent arrivals** (last 45 minutes)
   - Record ages of non-disabled pilgrims
   - Count disabled fraction

2. **Compute age cutoff**
   ```
   If we want 35% for Level 1, and 10% are disabled:
   → 25% of non-disabled should go to Level 1
   → Top 25% oldest = 75th percentile
   → Age cutoff = 75th percentile of recent ages
   ```

3. **Assign levels**
   - Disabled → Level 1
   - Age ≥ cutoff → Level 1 (unless severely overcrowded)
   - Age < cutoff → Least crowded of Level 2/3

---

## Configuration

Only one main parameter to tune:

**`alpha1`** (default: 0.35)
- Target fraction for Level 1
- Higher = more people to Level 1 = lower age cutoff
- Lower = fewer people to Level 1 = higher age cutoff

```bash
# Set to 40% for Level 1
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"alpha1": 0.40}'
```

---

## API Usage

### Assign a Pilgrim
```bash
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 45, "isDisabled": false}'
```

Response shows:
- Assigned level
- Current age cutoff
- Occupancy at each level
- Reason for decision

### Check Metrics
```bash
curl http://localhost:5000/api/LoadBalancer/metrics
```

Shows:
- Current age cutoff
- Disabled fraction
- Occupancy counts
- Age quantiles (50th, 80th, 90th percentile)

---

## Why This Is Better Than Fixed Thresholds

### Fixed Threshold (e.g., "age >= 60")
- ❌ Doesn't adapt to population
- ❌ If everyone is 70+, Level 1 gets overwhelmed
- ❌ If everyone is 30-40, Level 1 stays empty

### Dynamic Quantile-Based
- ✅ Adapts to actual ages
- ✅ Always distributes ~35% to Level 1 (configurable)
- ✅ Works with any age distribution

---

## What About Wait Times?

**There are NO wait times!**

People walk in, perform 45-minute ritual, leave. They don't wait.

The system tracks:
- ✅ Occupancy (how many people at each level)
- ❌ NOT wait times
- ❌ NOT queue lengths (people don't queue)
- ❌ NOT throughput calculations

The `waitEst` field in responses is a **misnomer** (kept for backward compatibility) - it actually shows **occupancy count**.

---

## Examples

### See It Adapt to Different Crowds

```bash
# Test with young crowd
for age in 25 30 35 40 45 50; do
  curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
    -H "Content-Type: application/json" \
    -d "{\"age\": $age, \"isDisabled\": false}" -s | jq '.decision.ageCutoff'
done

# Age cutoff should be low (around 40-45)

# Test with older crowd
for age in 60 65 70 75 80 85; do
  curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
    -H "Content-Type: application/json" \
    -d "{\"age\": $age, \"isDisabled\": false}" -s | jq '.decision.ageCutoff'
done

# Age cutoff should rise (to around 75-80)
```

---

## Full Documentation

See `QUANTILE_BASED_LOAD_BALANCER.md` for complete details on:
- Algorithm explanation
- Configuration parameters
- API reference
- Tuning guidelines
- Example scenarios

---

## Summary

**Problem:** Need to distribute pilgrims based on age, but age distribution varies

**Solution:** Use quantiles to compute a dynamic age cutoff that adapts to the actual population

**Result:** Always achieves target distribution (e.g., 35% to Level 1) regardless of whether the crowd is young, old, or mixed
