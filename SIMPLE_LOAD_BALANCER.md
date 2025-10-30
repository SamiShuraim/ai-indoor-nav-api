# Simple Occupancy-Based Load Balancer

## Overview

This is a **simple occupancy-based load balancer** for distributing pilgrims across 3 levels to minimize crowding.

### The Problem

- 3 levels (floors/areas) for performing rituals
- Everyone spends **exactly 45 minutes** then leaves
- **NO waiting** - people just walk in and perform the ritual
- Problem: Avoid overcrowding any single level
- Preference: Disabled and older people should go to Level 1 (easier access)

**This is NOT a queueing problem - it's a simple CAPACITY/OCCUPANCY problem!**

---

## How It Works

### Assignment Rules

1. **Disabled pilgrims** → Always assigned to Level 1 (priority access)
2. **Older pilgrims (age ≥ 60)** → Prefer Level 1, but redirect to 2/3 if Level 1 is over capacity
3. **Younger pilgrims (age < 60)** → Assign to least crowded of Level 2 or Level 3

### What We Track

**Only ONE thing:** Current occupancy (how many people are currently at each level)

- When someone is assigned, they arrive immediately
- They stay for exactly 45 minutes
- They automatically leave after 45 minutes
- System automatically tracks this lifecycle

---

## API Endpoints

### 1. Assign Level (Primary Endpoint)

**POST** `/api/LoadBalancer/arrivals/assign`

Assigns a pilgrim to a level.

**Request:**
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
    "ageCutoff": 60,
    "waitEst": {
      "1": 45,  // 45 people currently at Level 1
      "2": 38,  // 38 people currently at Level 2
      "3": 52   // 52 people currently at Level 3
    },
    "reason": "Age 45 < 60, assigned to least crowded level"
  },
  "traceId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Note:** `waitEst` is misleading name (kept for backward compatibility) - it actually shows **occupancy counts**, not wait times.

---

### 2. Get Metrics

**GET** `/api/LoadBalancer/metrics`

Gets current occupancy at all levels.

**Response:**
```json
{
  "ageCutoff": 60,
  "counts": {
    "total": 135
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

---

### 3. Update Configuration

**POST** `/api/LoadBalancer/config`

Updates configuration at runtime.

**Request:**
```json
{
  "ageThreshold": 65,
  "level1TargetShare": 0.35
}
```

**Response:**
```json
{
  "ageThreshold": 65,
  "level1TargetShare": 0.35
}
```

**Configuration Parameters:**

| Parameter | Default | Description |
|-----------|---------|-------------|
| `ageThreshold` | 60 | Age at which pilgrims prefer Level 1 |
| `level1TargetShare` | 0.40 | Target share for Level 1 (e.g., 0.4 = 40%) |

---

### 4. Get Configuration

**GET** `/api/LoadBalancer/config`

Gets current configuration.

---

### 5. Health Check

**GET** `/api/LoadBalancer/health`

Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-30T12:34:56Z"
}
```

---

## Assignment Logic (Pseudocode)

```
Get current occupancy:
  level1Count, level2Count, level3Count
  totalCount = sum of all levels

IF pilgrim is disabled:
    ASSIGN Level 1
    REASON: "Disabled pilgrims always assigned to Level 1"

ELSE IF pilgrim age >= ageThreshold (default 60):
    IF level1Count < totalCount * level1TargetShare:
        ASSIGN Level 1
        REASON: "Age >= threshold, Level 1 under capacity"
    ELSE:
        ASSIGN least crowded of Level 2 or 3
        REASON: "Age >= threshold but Level 1 over capacity"

ELSE (younger pilgrim):
    ASSIGN least crowded of Level 2 or 3
    REASON: "Age < threshold, assigned to least crowded"
```

---

## Configuration Tuning

### Age Threshold

- **Default: 60 years**
- Higher value (e.g., 65) → Fewer people prefer Level 1 → More balanced distribution
- Lower value (e.g., 55) → More people prefer Level 1 → Level 1 gets busier

### Level 1 Target Share

- **Default: 0.40 (40%)**
- Controls when elderly are redirected from Level 1
- Higher value (e.g., 0.50) → Level 1 can hold more people before redirecting
- Lower value (e.g., 0.30) → Level 1 redirects elderly sooner

---

## Testing

### Test with curl

```bash
# Assign a disabled pilgrim (should always get Level 1)
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 45, "isDisabled": true}'

# Assign an elderly pilgrim (should prefer Level 1)
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 70, "isDisabled": false}'

# Assign a young pilgrim (should go to Level 2 or 3)
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 30, "isDisabled": false}'

# Check current occupancy
curl http://localhost:5000/api/LoadBalancer/metrics
```

### Expected Behavior

1. **All disabled pilgrims** get Level 1
2. **Elderly pilgrims** get Level 1 until it reaches ~40% of total occupancy
3. **Young pilgrims** are balanced between Level 2 and Level 3
4. **After 45 minutes**, people automatically leave and occupancy decreases

---

## Legacy Endpoints (Backward Compatibility)

These endpoints still exist but are deprecated:

- `POST /api/LoadBalancer/assign` - Use `/arrivals/assign` instead
- `GET /api/LoadBalancer/utilization` - Use `/metrics` instead
- `POST /api/LoadBalancer/reset` - No longer applicable
- `POST /api/LoadBalancer/levels/state` - No longer needed (occupancy tracked automatically)
- `POST /api/LoadBalancer/control/tick` - No longer applicable

---

## What Was Removed

The previous system had unnecessary complexity for a simple occupancy problem:

### Removed Features:
- ❌ Wait time calculations and tracking
- ❌ Throughput calculations
- ❌ Feedback controllers
- ❌ Dynamic age cutoffs based on quantiles
- ❌ Rolling statistics and percentile estimation
- ❌ Soft gates and randomization
- ❌ Controller gain, alpha1, p_disabled parameters
- ❌ Level state updates from external sources

### What Remains:
- ✅ Simple occupancy tracking (via `LevelTracker`)
- ✅ 45-minute automatic lifecycle management
- ✅ Simple assignment rules based on age, disability, and current occupancy
- ✅ Two configuration parameters: age threshold and target share

---

## Summary

**Before:** Complex feedback controller with 10+ parameters, rolling statistics, quantile estimation, wait time targeting, and hundreds of lines of logic.

**After:** ~150 lines of simple code that tracks occupancy and uses 3 basic rules.

**Result:** Solves the actual problem (distribute people to avoid crowding) without unnecessary complexity.

---

## Mobile App Integration

The mobile app just needs to call one endpoint:

```
POST /api/LoadBalancer/arrivals/assign
{
  "age": <user_age>,
  "isDisabled": <true/false>
}

Response:
{
  "level": <1/2/3>  // Show this to the user
}
```

That's it!

---

## Questions?

For more details, see:
- API documentation: `/swagger` (when running in development)
- Source code: `Services/LoadBalancerService.cs` (~150 lines)
