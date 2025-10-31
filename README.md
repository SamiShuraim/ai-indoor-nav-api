# Adaptive Load Balancer - Capacity-Based with Soft Gate

## Overview

This load balancer distributes pilgrims across 3 levels using:
- **Hard capacity limits** for each level
- **Dynamic age cutoff** computed from quantiles
- **Utilization-based feedback controller** that adjusts target share
- **Sigmoid soft gate** around the age cutoff for smooth transitions
- **Per-minute rate limiting** to prevent bursts

---

## System Behavior

### Capacity Limits
- **Level 1 (Soft):** 500 people
- **Level 1 (Hard):** 550 people (disabled overflow only)
- **Level 2:** 3,000 people
- **Level 3:** 3,000 people

### Dwell Time
- Everyone stays for **45 minutes** then automatically leaves
- System tracks active assignments with expiry

### Assignment Rules

**Disabled pilgrims:**
- Always go to Level 1 if under hard cap (550) and rate limit
- Otherwise overflow to Level 2/3

**Non-disabled pilgrims:**
- If Level 1 at soft cap (500) or rate limited → Level 2/3
- Otherwise, apply **soft gate** based on age vs dynamic cutoff
- Sigmoid function provides smooth probability:
  - Ages far above cutoff → ~100% chance of Level 1
  - Ages far below cutoff → ~0% chance of Level 1
  - Ages near cutoff → ~50% chance (smooth transition)

---

## Dynamic Age Cutoff

The age cutoff adapts to the population:

```
Given:
- alpha1 = target share for Level 1 (adjusted by controller)
- p_disabled = fraction of disabled arrivals (measured)

Compute:
- share_left_for_old = alpha1 - p_disabled
- tau = 1 - share_left_for_old
- age_cutoff = tau-quantile of recent non-disabled ages
```

**Example:**
- alpha1 = 0.08 (controller wants 8% to Level 1)
- p_disabled = 0.02 (2% are disabled)
- share_left_for_old = 0.06 (6% for elderly)
- tau = 0.94 (94th percentile)
- age_cutoff = 94th percentile of recent ages → e.g., 72

---

## Feedback Controller

Runs every minute to adjust alpha1 based on L1 utilization:

```
util = active_L1 / L1_cap_soft
error = target_util (0.90) - util
alpha1 = alpha1 + gain * error
alpha1 = clamp(alpha1, max(alpha1_min, p_disabled), alpha1_max)
```

**Behavior:**
- If L1 under-utilized → increase alpha1 → lower age cutoff → more people to L1
- If L1 over-utilized → decrease alpha1 → raise age cutoff → fewer people to L1

**Target:** Keep Level 1 at 90% of soft capacity (450/500 people)

---

## Soft Gate (Sigmoid Function)

Instead of a hard cutoff, uses a smooth probability:

```
z = (age - age_cutoff) / band_width (3 years)
p_L1 = 1 / (1 + exp(-z))
```

**Zones:**
- `age >= cutoff + 6`: p = 100% (deterministic)
- `age in [cutoff-6, cutoff+6]`: p = sigmoid (smooth)
- `age <= cutoff - 6`: p = 0% (deterministic)

**Guard:** If recent share exceeds target, reduce probability to prevent overshoot.

---

## Rate Limiting

Per-minute limit for Level 1:
```
rate_limit = floor(L1_cap_soft / dwell_minutes) = floor(500 / 45) = 11 per minute
```

Prevents bursts from overwhelming Level 1.

---

## API Usage

### Assign Pilgrim

**POST** `/api/LoadBalancer/arrivals/assign`

```json
{
  "age": 65,
  "isDisabled": false
}
```

**Response:**
```json
{
  "level": 1,
  "decision": {
    "age": 65,
    "isDisabled": false,
    "ageCutoff": 62.3,
    "alpha1": 0.078,
    "pDisabled": 0.02,
    "shareLeftForOld": 0.058,
    "tauQuantile": 0.942,
    "occupancy": {
      "1": 445,
      "2": 1250,
      "3": 1380
    },
    "reason": "soft-gate pass (p=0.845, r=0.234)"
  },
  "traceId": "..."
}
```

### Get Metrics

**GET** `/api/LoadBalancer/metrics`

```json
{
  "alpha1": 0.078,
  "ageCutoff": 62.3,
  "pDisabled": 0.02,
  "counts": {
    "total": 520,
    "disabled": 12,
    "nonDisabled": 508
  },
  "quantilesNonDisabledAge": {
    "q50": 44.2,
    "q80": 58.1,
    "q90": 68.5
  },
  "levels": {
    "1": { "occupancy": 445 },
    "2": { "occupancy": 1250 },
    "3": { "occupancy": 1380 }
  }
}
```

### Update Config

**POST** `/api/LoadBalancer/config`

```json
{
  "alpha1": 0.09,
  "alpha1Min": 0.05,
  "alpha1Max": 0.12
}
```

---

## Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `L1CapSoft` | 500 | Soft capacity for Level 1 |
| `L1CapHard` | 550 | Hard capacity (disabled overflow) |
| `L2Cap` | 3000 | Capacity for Level 2 |
| `L3Cap` | 3000 | Capacity for Level 3 |
| `DwellMinutes` | 45 | How long people stay |
| `TargetAlpha1` | 0.0769 | Initial target share for L1 |
| `Alpha1Min` | 0.05 | Minimum alpha1 |
| `Alpha1Max` | 0.12 | Maximum alpha1 |
| `TargetUtilL1` | 0.90 | Target utilization (90% of soft cap) |
| `ControllerGain` | 0.05 | Feedback gain |
| `SoftGateBandYears` | 3.0 | Sigmoid band width |
| `RecentShareWindowMinutes` | 10 | Window for recent share tracking |
| `RecentShareGuard` | 0.02 | Overshoot guard threshold |

---

## Key Features

### ✅ Capacity Protection
- Hard limits prevent overflow
- Rate limiting prevents bursts
- Disabled guaranteed Level 1 (up to hard cap)

### ✅ Adaptive Cutoff
- Age threshold adapts to population
- Works with young crowds, old crowds, mixed crowds
- Responds to varying disabled fractions

### ✅ Smooth Transitions
- Sigmoid soft gate eliminates hard cutoff jumps
- Probabilistic assignment in boundary zone
- Deterministic for ages far from cutoff

### ✅ Utilization Control
- Feedback controller targets 90% utilization
- Automatically adjusts alpha1 every minute
- Prevents under/over-utilization of Level 1

### ✅ Safety
- Rate limiting prevents instant overshoot
- Recent share guard prevents overshooting target
- Disabled overflow to L2/L3 when L1 full

---

## Example Scenarios

### Scenario 1: Normal Operation
```
Active: L1=445, L2=1250, L3=1380
Utilization: 445/500 = 89% (near target)
alpha1: 0.078 (stable)
Cutoff: 62 years
Arrival age 65: p=0.85 → likely Level 1
Arrival age 55: p=0.15 → likely Level 2/3
```

### Scenario 2: Under-Utilization
```
Active: L1=200, L2=2000, L3=2100
Utilization: 200/500 = 40% (below target)
Controller: increase alpha1 → lower cutoff
Next minute: cutoff drops from 65 → 58
More people now eligible for Level 1
```

### Scenario 3: Over-Utilization
```
Active: L1=490, L2=800, L3=900
Utilization: 490/500 = 98% (above target)
Controller: decrease alpha1 → raise cutoff
Next minute: cutoff rises from 60 → 68
Fewer people eligible for Level 1
```

### Scenario 4: Disabled Surge
```
Many disabled arrive (p_disabled = 0.10)
alpha1 = 0.08, share_left_for_old = -0.02 → 0
Cutoff → very high (only most elderly get L1)
Most non-disabled → Level 2/3
Level 1 reserved for disabled
```

---

## Quick Start

```bash
# Run the application
dotnet run

# Assign a pilgrim
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 65, "isDisabled": false}'

# Check metrics
curl http://localhost:5000/api/LoadBalancer/metrics

# Update config
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"alpha1": 0.09}'
```

---

## Implementation Details

### Core Components

- **AssignmentLog**: Tracks active assignments with automatic expiry
- **RateLimiter**: Per-minute rate limiting for Level 1
- **RollingQuantileEstimator**: Computes percentiles of recent ages
- **RollingCounts**: Tracks disabled fraction
- **LoadBalancerService**: Main logic with controller and soft gate

### Controller Loop

Runs every minute:
1. Evict expired assignments
2. Compute L1 utilization
3. Adjust alpha1 based on error
4. Recompute age cutoff
5. Update recent share (alpha1_hat)

### Assignment Flow

1. Evict expired assignments
2. Get active counts and rate limit
3. If disabled → L1 (or overflow)
4. If non-disabled:
   - Check capacity and rate limit
   - Compute soft gate probability
   - Draw random number
   - Assign based on result
5. Record assignment
6. Update statistics

---

## Summary

**Problem:** Distribute pilgrims to minimize crowding while prioritizing disabled and elderly

**Solution:** 
- Capacity-based system with hard limits
- Dynamic age cutoff from quantiles
- Utilization feedback controller
- Sigmoid soft gate for smooth transitions
- Rate limiting for burst protection

**Result:** Adaptive system that maintains target utilization, adapts to population, and provides smooth, probabilistic assignments
