# Quantile-Based Occupancy Load Balancer

## Overview

This load balancer uses **dynamic age cutoffs** computed from the distribution of recent arrivals to distribute pilgrims across 3 levels and minimize crowding.

### Key Principles

1. **NO fixed age threshold** - Age cutoff adapts to the actual ages of arriving pilgrims
2. **NO wait times** - People walk in, perform ritual for 45 minutes, leave
3. **Occupancy-based** - Track how many people are at each level
4. **Distribution-driven** - Use quantiles of recent arrivals to compute age cutoff

---

## How It Works

### The Algorithm

#### Step 1: Track Recent Arrivals
- Keep a rolling window (default: 45 minutes) of recent arrivals
- Track disabled fraction: `p_disabled`
- Track ages of non-disabled pilgrims

#### Step 2: Compute Dynamic Age Cutoff

```
alpha1 = target share for Level 1 (configurable, default 0.35 = 35%)
p_disabled = fraction of disabled arrivals
share_left_for_old = alpha1 - p_disabled
tau = 1 - share_left_for_old
age_cutoff = tau-quantile of non-disabled ages
```

**Example:**
- `alpha1 = 0.35` (want 35% of arrivals to go to Level 1)
- `p_disabled = 0.15` (15% of arrivals are disabled)
- `share_left_for_old = 0.35 - 0.15 = 0.20` (20% left for elderly)
- `tau = 1 - 0.20 = 0.80` (80th percentile)
- `age_cutoff = 80th percentile of recent non-disabled ages`

**Intuition:**
- If 15% are disabled and we want 35% total for Level 1
- Then 20% of non-disabled should go to Level 1
- That means the top 20% oldest non-disabled → Level 1
- Which is the 80th percentile cutoff

#### Step 3: Assign Based on Cutoff and Occupancy

```
IF disabled:
    → Level 1

ELSE IF age >= age_cutoff:
    IF Level 1 is severely overcrowded:
        → Least crowded of Level 2/3
    ELSE:
        → Level 1

ELSE:
    → Least crowded of Level 2/3
```

---

## Why This Approach?

### Adapts to the Population

**Scenario 1: Mostly young arrivals**
- Recent arrivals: ages 20-40
- 80th percentile might be age 38
- Age cutoff = 38
- Anyone 38+ goes to Level 1

**Scenario 2: Older crowd**
- Recent arrivals: ages 50-80
- 80th percentile might be age 72
- Age cutoff = 72
- Only 72+ go to Level 1

**Scenario 3: Mixed ages**
- Recent arrivals: ages 20-90
- 80th percentile might be age 65
- Age cutoff = 65
- 65+ go to Level 1

### Handles Variable Disabled Fraction

**If more disabled arrive:**
- `p_disabled` increases (e.g., 0.15 → 0.25)
- `share_left_for_old` decreases (0.20 → 0.10)
- `tau` increases (0.80 → 0.90)
- Age cutoff rises (fewer non-disabled elderly fit in Level 1)

**If fewer disabled arrive:**
- `p_disabled` decreases (e.g., 0.15 → 0.05)
- `share_left_for_old` increases (0.20 → 0.30)
- `tau` decreases (0.80 → 0.70)
- Age cutoff drops (more non-disabled elderly can fit in Level 1)

---

## Configuration

### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `alpha1` | 0.35 | Target share for Level 1 (0-1) |
| `alpha1Min` | 0.15 | Minimum alpha1 |
| `alpha1Max` | 0.55 | Maximum alpha1 |
| `slidingWindowMinutes` | 45 | Rolling window for tracking arrivals |
| `windowMode` | "sliding" | "sliding" or "decay" |
| `halfLifeMinutes` | 45 | Half-life for decay mode |

### Tuning Alpha1

**Higher alpha1 (e.g., 0.45):**
- More people go to Level 1
- Lower age cutoff
- Level 1 gets busier

**Lower alpha1 (e.g., 0.25):**
- Fewer people go to Level 1
- Higher age cutoff
- Level 1 less busy, but only most elderly get in

**Recommended:** Start with 0.35 and adjust based on observed distribution.

---

## API Endpoints

### 1. Assign Level

**POST** `/api/LoadBalancer/arrivals/assign`

```json
{
  "age": 45,
  "isDisabled": false
}
```

**Response:**
```json
{
  "level": 2,
  "decision": {
    "isDisabled": false,
    "age": 45,
    "ageCutoff": 62.3,
    "alpha1": 0.35,
    "pDisabled": 0.15,
    "shareLeftForOld": 0.20,
    "tauQuantile": 0.80,
    "waitEst": {
      "1": 45,  // Occupancy count (not wait time!)
      "2": 38,
      "3": 52
    },
    "reason": "Age 45 < cutoff (62.3); assigned to less crowded level"
  },
  "traceId": "..."
}
```

### 2. Get Metrics

**GET** `/api/LoadBalancer/metrics`

```json
{
  "alpha1": 0.35,
  "pDisabled": 0.15,
  "ageCutoff": 62.3,
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
    "1": {
      "queueLength": 45,
      "waitEst": 45
    },
    "2": {
      "queueLength": 38,
      "waitEst": 38
    },
    "3": {
      "queueLength": 52,
      "waitEst": 52
    }
  }
}
```

**Note:** `waitEst` is a legacy field name - it actually shows occupancy count.

### 3. Update Config

**POST** `/api/LoadBalancer/config`

```json
{
  "alpha1": 0.40,
  "window": {
    "mode": "sliding",
    "minutes": 60
  }
}
```

---

## Example Scenarios

### Cold Start (No History)

```
Situation: First few arrivals, no age history yet
- age_cutoff = -∞ (no data)
- Disabled → Level 1
- All others → Balanced between 2/3
```

### After Some Arrivals

```
Arrivals (last 45 min): Ages 25,30,35,40,45,50,55,60,65,70,75,80
Disabled: 3 out of 12 → p_disabled = 0.25

alpha1 = 0.35
share_left_for_old = 0.35 - 0.25 = 0.10
tau = 0.90
age_cutoff = 90th percentile of [25,30,35,40,45,50,55,60,65] = 65

Result:
- Disabled → Level 1
- Age 65+ → Level 1
- Age < 65 → Level 2 or 3
```

### Peak with Many Disabled

```
Arrivals: 40% disabled (p_disabled = 0.40)

alpha1 = 0.35
share_left_for_old = 0.35 - 0.40 = -0.05 → 0 (capped at 0)
tau = 1.0
age_cutoff = 100th percentile = maximum age in recent arrivals

Result:
- Level 1 is full with disabled
- Only the absolute oldest non-disabled (if any room) go to Level 1
- Almost all non-disabled → Level 2/3
```

---

## Differences from Previous System

### ✅ Kept
- Dynamic age cutoff based on quantiles
- Rolling statistics
- Alpha1 (target share for Level 1)
- Tracks disabled fraction (p_disabled)

### ❌ Removed
- Wait time calculations and tracking
- Throughput calculations
- Feedback controller (adjusting alpha1 based on wait times)
- Wait time targets
- Controller gain
- Soft gates and randomization
- Level state updates from external sources

### 🔄 Changed
- Alpha1 is now **user-configured** (not adjusted by controller)
- System tracks **occupancy** (not wait times)
- Decisions based on **occupancy** (not wait estimates)

---

## Testing

### Verify Dynamic Cutoff

```bash
# Simulate arrivals with different age distributions
# Young crowd:
for i in {1..20}; do
  curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
    -H "Content-Type: application/json" \
    -d "{\"age\": $((20 + RANDOM % 25)), \"isDisabled\": false}"
done

# Check cutoff (should be low, around 40)
curl http://localhost:5000/api/LoadBalancer/metrics | jq '.ageCutoff'

# Old crowd:
for i in {1..20}; do
  curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
    -H "Content-Type: application/json" \
    -d "{\"age\": $((60 + RANDOM % 25)), \"isDisabled\": false}"
done

# Check cutoff (should be high, around 75)
curl http://localhost:5000/api/LoadBalancer/metrics | jq '.ageCutoff'
```

---

## Summary

**What it does:**
- Computes age cutoff from the distribution of recent arrivals
- Adapts automatically to the population (young vs old)
- Adjusts for varying disabled fractions
- Uses occupancy to avoid extreme crowding

**What it doesn't do:**
- Calculate wait times (there are none!)
- Track throughput
- Adjust alpha1 automatically (user configures it)
- Use feedback controllers

**Result:** A distribution-driven system that adapts to the actual population while keeping it simple and focused on occupancy management.
