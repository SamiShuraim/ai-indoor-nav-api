# Adaptive Load Balancer Implementation

## Overview

This is a complete rebuild of the load balancer service that implements an adaptive, data-driven approach to pilgrim level assignment for tawaf (Hajj pilgrimage). Unlike the previous system with fixed age thresholds, this system uses rolling statistics, streaming quantile estimation, and a feedback controller to dynamically adjust assignments based on real-time congestion and arrival patterns.

## Key Features

### 1. **No Fixed Age Thresholds**
- Age cutoff is computed dynamically from the current distribution of arriving pilgrims
- Adapts automatically to changes in the mix of disabled/non-disabled arrivals
- Uses streaming quantile estimation (percentile-based cutoffs)

### 2. **Feedback Controller**
- Maintains Level 1 stability by targeting a desired wait time (default: 12 minutes)
- Adjusts `alpha1` (target fraction sent to Level 1) based on actual wait times
- Automatically increases/decreases Level 1 assignments to meet performance targets

### 3. **Rolling Statistics**
- Tracks arrivals over a configurable time window (default: 45 minutes)
- Supports both sliding window and exponential decay modes
- Maintains streaming quantile estimates for non-disabled pilgrim ages

### 4. **Priority Rules**
- **Disabled pilgrims**: Always assigned to Level 1 (highest priority)
- **Elderly pilgrims**: Assigned to Level 1 if age ≥ dynamic cutoff
- **Younger pilgrims**: Assigned to the less busy of Levels 2 or 3

### 5. **Soft Gates and Randomization**
- Soft gate prevents overshooting Level 1 target when demand spikes
- Small randomization (5-10%) in boundary band prevents oscillations
- Configurable and can be disabled

## Architecture

### Core Components

```
LoadBalancerService
├── LoadBalancerConfig         # Runtime configuration
├── RollingQuantileEstimator   # Streaming quantile calculation
├── RollingCounts              # Track arrival counts over time
├── LevelStateManager          # Track wait times, queues, throughput
└── AdaptiveController         # Feedback control & routing logic
```

### Component Details

#### **RollingQuantileEstimator**
- Maintains timestamped values of non-disabled pilgrim ages
- Computes percentiles (e.g., 80th percentile for age cutoff)
- Supports sliding window or exponential decay modes
- Thread-safe with automatic cleanup of old data

#### **RollingCounts**
- Tracks total, disabled, and non-disabled arrival counts
- Computes `p_disabled` (fraction of disabled arrivals)
- Uses same window mode as quantile estimator

#### **LevelStateManager**
- Stores latest wait estimates, queue lengths, throughput per level
- Falls back to derived estimates if data is missing
- Thread-safe concurrent access

#### **AdaptiveController**
- Implements the feedback control loop
- Computes dynamic age cutoff from quantiles
- Routes individual arrivals to levels
- Tracks recent assignments for soft gate

#### **LoadBalancerConfig**
- All parameters are runtime configurable
- Validates parameter ranges
- Can be updated via REST API

## API Endpoints

### Primary Endpoints

#### 1. **POST /api/LoadBalancer/arrivals/assign**
Assigns a pilgrim to a level.

**Request:**
```json
{
  "age": 68,
  "isDisabled": false
}
```

**Response:**
```json
{
  "level": 1,
  "decision": {
    "isDisabled": false,
    "age": 68,
    "ageCutoff": 66.9,
    "alpha1": 0.37,
    "pDisabled": 0.19,
    "shareLeftForOld": 0.18,
    "tauQuantile": 0.82,
    "waitEst": {
      "1": 11.2,
      "2": 15.0,
      "3": 14.3
    },
    "reason": "age ≥ dynamic cutoff; Level 1 within target share"
  },
  "traceId": "550e8400-e29b-41d4-a716-446655440000"
}
```

#### 2. **POST /api/LoadBalancer/levels/state**
Updates level state (wait times, queue lengths, throughput).

**Request:**
```json
{
  "levels": [
    {
      "level": 1,
      "waitEst": 13.1,
      "queueLen": 120,
      "throughputPerMin": 10.5
    },
    {
      "level": 2,
      "waitEst": 15.2,
      "queueLen": 230,
      "throughputPerMin": 18.0
    },
    {
      "level": 3,
      "waitEst": 14.7,
      "queueLen": 210,
      "throughputPerMin": 17.2
    }
  ]
}
```

**Response:**
```json
{
  "ok": true
}
```

#### 3. **POST /api/LoadBalancer/control/tick**
Manually triggers a controller tick (normally runs automatically every minute).

**Response:**
```json
{
  "alpha1": 0.36,
  "ageCutoff": 64.8,
  "pDisabled": 0.21,
  "window": {
    "method": "sliding",
    "slidingWindowMinutes": 45
  }
}
```

#### 4. **GET /api/LoadBalancer/metrics**
Gets comprehensive metrics and statistics.

**Response:**
```json
{
  "alpha1": 0.36,
  "alpha1Min": 0.15,
  "alpha1Max": 0.55,
  "waitTargetMinutes": 12,
  "controllerGain": 0.03,
  "pDisabled": 0.21,
  "ageCutoff": 64.8,
  "counts": {
    "total": 1820,
    "disabled": 392,
    "nonDisabled": 1428
  },
  "quantilesNonDisabledAge": {
    "q50": 44.1,
    "q80": 60.2,
    "q90": 66.5
  },
  "levels": {
    "1": { "waitEst": 12.6 },
    "2": { "waitEst": 15.2 },
    "3": { "waitEst": 14.7 }
  }
}
```

#### 5. **POST /api/LoadBalancer/config**
Updates runtime configuration.

**Request (any subset):**
```json
{
  "alpha1": 0.4,
  "alpha1Min": 0.15,
  "alpha1Max": 0.55,
  "waitTargetMinutes": 10,
  "controllerGain": 0.03,
  "window": {
    "mode": "sliding",
    "minutes": 45
  },
  "softGate": {
    "enabled": true,
    "bandYears": 3.0
  },
  "randomization": {
    "enabled": true,
    "rate": 0.07
  }
}
```

**Response:**
Full resolved configuration after update.

#### 6. **GET /api/LoadBalancer/config**
Gets current configuration.

#### 7. **GET /api/LoadBalancer/health**
Health check endpoint.

### Legacy Endpoints (Backward Compatibility)

- `POST /api/LoadBalancer/assign` - Old assignment endpoint
- `GET /api/LoadBalancer/utilization` - Old utilization endpoint
- `POST /api/LoadBalancer/reset` - Old reset endpoint

These are maintained for backward compatibility but may not reflect full system state.

## Algorithm Details

### Controller Update Rule (runs every minute)

```
error = waitTargetMinutes - waitEst[1]
alpha1_new = alpha1 + controllerGain * error
lowerBound = max(alpha1Min, p_disabled)
alpha1_new = clip(alpha1_new, lowerBound, alpha1Max)
```

**Intuition:**
- If Level 1 wait time is above target → reduce alpha1 (send fewer to Level 1)
- If Level 1 wait time is below target → increase alpha1 (send more to Level 1)
- Lower bound ensures enough capacity for disabled pilgrims

### Dynamic Age Cutoff Calculation

```
shareLeftForOld = max(0, alpha1 - p_disabled)
tau = 1 - shareLeftForOld
ageCutoff = quantile(tau) of non-disabled ages
```

**Intuition:**
- After reserving capacity for disabled pilgrims, remaining capacity goes to elderly
- The cutoff automatically rises when more disabled arrive (less room for elderly)
- The cutoff automatically falls when fewer disabled arrive (more room for elderly)

### Per-Arrival Routing Logic

```
if isDisabled:
    return Level 1
else if age >= ageCutoff:
    if softGateActive and inBoundaryBand:
        return lessBusy(Level 2, Level 3)
    else:
        return Level 1
else:
    return lessBusy(Level 2, Level 3)
```

## Configuration Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `alpha1` | 0.35 | [0, 1] | Target fraction sent to Level 1 |
| `alpha1Min` | 0.15 | [0, 1] | Minimum alpha1 |
| `alpha1Max` | 0.55 | [0, 1] | Maximum alpha1 |
| `waitTargetMinutes` | 12 | > 0 | Target wait time for Level 1 |
| `controllerGain` | 0.03 | > 0 | Controller feedback gain (k) |
| `slidingWindowMinutes` | 45 | > 0 | Rolling window duration |
| `halfLifeMinutes` | 45 | > 0 | Half-life for exponential decay mode |
| `windowMode` | "sliding" | sliding/decay | Window calculation mode |
| `softGateEnabled` | true | bool | Enable soft gate |
| `softGateBandYears` | 3.0 | ≥ 0 | Boundary band width in years |
| `randomizationEnabled` | true | bool | Enable randomization |
| `randomizationRate` | 0.07 | [0, 1] | Randomization probability |

## Testing

### Test Script

Run the included test script:

```bash
./test-adaptive-load-balancer.sh
```

This tests:
- Health check
- Initial metrics and config
- Disabled pilgrim assignment (must go to Level 1)
- Multiple non-disabled assignments with varying ages
- Level state updates
- Controller tick
- Configuration updates
- Edge cases (invalid ages)
- Batch assignments
- Legacy endpoint compatibility

### Key Test Cases

1. **Disabled Guarantee**: Any `isDisabled=true` must return Level 1
2. **Cutoff Monotonicity**: When `p_disabled` increases, `ageCutoff` increases
3. **Controller Direction**: 
   - If `waitEst[1] > target`, next `alpha1` decreases
   - If `waitEst[1] < target`, next `alpha1` increases
4. **Cold Start**: With no history, only disabled go to Level 1
5. **Load Balance**: Ages below cutoff go to less busy of Levels 2/3

### Manual Testing with curl

```bash
# Assign a pilgrim
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 65, "isDisabled": false}'

# Update level states
curl -X POST http://localhost:5000/api/LoadBalancer/levels/state \
  -H "Content-Type: application/json" \
  -d '{"levels":[{"level":1,"waitEst":13.5}]}'

# Get metrics
curl -X GET http://localhost:5000/api/LoadBalancer/metrics

# Update config
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"alpha1": 0.4}'
```

## Logging and Traceability

- Each assignment response includes a unique `traceId` (UUID)
- Detailed decision information in every response
- Controller state can be tracked via `/metrics` endpoint
- All responses include the reasoning for the assignment

## Non-Functional Requirements

✅ **Deterministic**: Given same inputs and state, produces same output (with fixed random seed)  
✅ **Thread-Safe**: All components use proper locking for concurrent access  
✅ **Validation**: Clear error messages for invalid inputs (age range, config bounds)  
✅ **Health Endpoint**: `/health` for monitoring service liveness  
✅ **No Fixed Thresholds**: Only uses quantiles and controller parameters  
✅ **Real-time Adaptation**: Controller runs every minute automatically  

## Migration from Old System

### Key Differences

| Old System | New System |
|------------|------------|
| Fixed age threshold (60+) | Dynamic age cutoff based on percentiles |
| Fixed priority scores | Adaptive controller adjusts to congestion |
| Capacity-based (hard limits) | Wait-time based (soft limits) |
| `isHealthy` boolean | `isDisabled` boolean (inverted logic) |
| Static configuration | Runtime-configurable parameters |
| No feedback loop | Feedback controller maintains targets |

### Breaking Changes

1. **Request Model**: `isHealthy` → `isDisabled` (inverted)
2. **Response Model**: Completely different structure with decision details
3. **Endpoint Path**: `/assign` → `/arrivals/assign` (legacy endpoint maintained)
4. **Utilization Tracking**: Not tracked in same way (use `/metrics` instead)

### Backward Compatibility

Legacy endpoints are maintained:
- `POST /api/LoadBalancer/assign` (maps `isHealthy` to `isDisabled`)
- `GET /api/LoadBalancer/utilization` (returns empty data)
- `POST /api/LoadBalancer/reset` (no-op in new system)

## Performance Considerations

- **Quantile Estimation**: Current implementation uses sorted list (O(n log n) per insert). For production at scale, consider implementing t-digest or Greenwald-Khanna algorithm.
- **Memory**: Rolling window keeps timestamped data in memory. Monitor memory usage with very high arrival rates.
- **Thread Safety**: All operations are locked. For very high concurrency, consider using lock-free data structures.
- **Periodic Tick**: Runs every minute. Can be adjusted if needed.

## Future Enhancements

1. **Persistence**: Save state to database for crash recovery
2. **Advanced Quantile Algorithms**: Implement t-digest for better performance
3. **Machine Learning**: Predictive models for arrival patterns
4. **Analytics Dashboard**: Real-time visualization of metrics
5. **Multi-Region**: Federated load balancing across multiple sites
6. **A/B Testing**: Compare different controller parameters
7. **Alert System**: Notifications when wait times exceed thresholds

## Acceptance Criteria

✅ Assigns levels according to adaptive algorithm with rolling statistics  
✅ Prioritizes disabled pilgrims for Level 1  
✅ Uses dynamic age cutoffs (no fixed thresholds)  
✅ Implements feedback controller targeting Level 1 wait time  
✅ Exposes all required API endpoints  
✅ Provides comprehensive metrics (alpha1, p_disabled, age_cutoff, quantiles, waits)  
✅ Adapts in real-time to changes in arrival mix and level congestion  
✅ Validates inputs with clear error messages  
✅ Deterministic behavior (given same inputs and state)  
✅ Thread-safe for concurrent requests  
✅ Health endpoint for monitoring  

## Support

For questions or issues, see:
- API documentation: `/swagger` (when running in development)
- Test script: `./test-adaptive-load-balancer.sh`
- Legacy documentation: `LOAD_BALANCER_IMPLEMENTATION.md`
