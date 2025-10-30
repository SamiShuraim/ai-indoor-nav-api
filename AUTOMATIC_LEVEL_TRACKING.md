# Automatic Level Tracking System

## Overview

The adaptive load balancer now includes **automatic level state tracking**. The mobile app only needs to call the assignment endpoint - all congestion tracking, queue management, and statistics are handled automatically by the backend.

## How It Works

### 1. Mobile App Assignment (The Only Step Required!)

```http
POST /api/LoadBalancer/arrivals/assign
Content-Type: application/json

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
    "ageCutoff": 42.5,
    "alpha1": 0.35,
    "pDisabled": 0.12,
    "shareLeftForOld": 0.23,
    "tauQuantile": 0.77,
    "waitEst": {
      "1": 13.2,
      "2": 11.5,
      "3": 12.8
    },
    "reason": "age ≥ dynamic cutoff; Level 1 within target share"
  },
  "traceId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
}
```

### 2. Automatic Backend Processing

When a pilgrim is assigned, the backend **automatically**:

1. ✅ Records the pilgrim's arrival at the assigned level
2. ✅ Tracks them for 45 minutes (assumed visit duration)
3. ✅ Automatically removes them after 45 minutes
4. ✅ Calculates real-time queue lengths for each level
5. ✅ Computes throughput (pilgrims/minute) based on recent arrivals
6. ✅ Estimates wait times using: `wait_time = queue_length / throughput`
7. ✅ Updates the adaptive controller every minute
8. ✅ Adjusts alpha1 and age_cutoff based on Level 1 congestion

### 3. Admin Dashboard Monitoring

The admin dashboard can view real-time statistics:

```http
GET /api/LoadBalancer/metrics
```

**Response:**
```json
{
  "alpha1": 0.35,
  "alpha1Min": 0.15,
  "alpha1Max": 0.55,
  "waitTargetMinutes": 12.0,
  "controllerGain": 0.03,
  "pDisabled": 0.12,
  "ageCutoff": 42.5,
  "counts": {
    "total": 150,
    "disabled": 18,
    "nonDisabled": 132
  },
  "quantilesNonDisabledAge": {
    "q50": 38.0,
    "q80": 55.0,
    "q90": 68.0
  },
  "levels": {
    "1": {
      "waitEst": 13.2,
      "queueLength": 45,
      "throughputPerMin": 3.4
    },
    "2": {
      "waitEst": 11.5,
      "queueLength": 52,
      "throughputPerMin": 4.5
    },
    "3": {
      "waitEst": 12.8,
      "queueLength": 48,
      "throughputPerMin": 3.8
    }
  }
}
```

## Architecture Components

### LevelTracker
- **Purpose**: Tracks active pilgrims at each level
- **Lifecycle**: Pilgrims arrive immediately upon assignment, depart after 45 minutes
- **Calculations**:
  - Queue Length: Count of active pilgrims
  - Throughput: Arrivals per minute over last 10 minutes
  - Wait Estimate: Queue length / throughput

### LoadBalancerService
- **Integration**: Uses LevelTracker to automatically update level states
- **Controller Tick**: Every minute, updates level states and runs adaptive controller
- **Assignment Flow**:
  1. Validate pilgrim age
  2. Record arrival in rolling statistics
  3. Route to appropriate level
  4. **Record arrival in LevelTracker** ← NEW!
  5. Return assignment response

### AdaptiveController
- **Feedback Loop**: Adjusts alpha1 based on Level 1 wait time vs target
- **Dynamic Cutoff**: Computes age cutoff based on arrival statistics
- **Features**: Soft gate, randomization, boundary band logic

## Simplified Assumptions

For this initial implementation, we make these simplifying assumptions:

1. **Immediate Arrival**: Pilgrims arrive at their level immediately upon assignment
   - No transit time between assignment and arrival
   - Simplifies tracking and reduces latency

2. **Fixed Duration**: All pilgrims spend exactly 45 minutes at their level
   - Realistic average for the use case
   - Automatic cleanup after 45 minutes
   - Can be adjusted in future if needed

3. **No Manual Check-ins**: Mobile app doesn't need to report arrivals/departures
   - Reduces mobile app complexity
   - Eliminates network calls for check-in/check-out
   - Backend handles everything automatically

## Future Enhancements

If more accuracy is needed later, consider:

### Option 1: Manual Check-in/Check-out
```http
POST /api/LoadBalancer/levels/checkin
{
  "level": 1,
  "action": "enter",  // or "exit"
  "pilgrimId": "uuid"
}
```

### Option 2: Variable Duration
Track actual visit duration per pilgrim and use historical averages.

### Option 3: Real-time Sensors
Integrate with physical sensors or cameras at each level for ground truth.

## Testing

The test script still includes manual level state updates for testing purposes:

```bash
# This is OPTIONAL for testing - not needed in production!
curl -X POST http://localhost:5000/api/LoadBalancer/levels/state \
  -H "Content-Type: application/json" \
  -d '{
    "levels": [
      {"level": 1, "waitEst": 13.1, "queueLen": 120, "throughputPerMin": 10.5}
    ]
  }'
```

In production, these updates happen automatically!

## Summary

**Before**: Mobile app assigns → Someone manually updates level states → Controller adjusts

**Now**: Mobile app assigns → ✨ Everything happens automatically! ✨

The mobile app's responsibility is just:
1. Call `/arrivals/assign` with age and disability status
2. Show the assigned level to the user
3. Done!

All tracking, statistics, and adaptive adjustments happen automatically in the background.
