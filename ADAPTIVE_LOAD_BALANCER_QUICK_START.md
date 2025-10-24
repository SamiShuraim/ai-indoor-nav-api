# Adaptive Load Balancer - Quick Start Guide

## What Was Implemented

A complete rebuild of the load balancer service that uses adaptive, data-driven algorithms for pilgrim level assignment. The system implements:

✅ **Rolling statistics** with streaming quantile estimation  
✅ **Feedback controller** that adjusts to real-time congestion  
✅ **Dynamic age cutoffs** (no fixed thresholds)  
✅ **Priority system** (disabled always go to Level 1)  
✅ **Soft gates and randomization** to prevent oscillations  
✅ **Runtime configuration** (all parameters can be updated via API)  
✅ **Comprehensive metrics and monitoring**  

## Files Created/Modified

### New Model Classes (in `/Models/`)
- `ArrivalAssignRequest.cs` - Request model for assignments
- `ArrivalAssignResponse.cs` - Response with detailed decision info
- `LevelStateUpdateRequest.cs` - Model for updating level states
- `ControlTickResponse.cs` - Controller tick response
- `MetricsResponse.cs` - Comprehensive metrics
- `ConfigUpdateRequest.cs` - Configuration update model

### New Service Components (in `/Services/`)
- `RollingQuantileEstimator.cs` - Streaming quantile calculation
- `RollingCounts.cs` - Rolling arrival counts
- `LevelStateManager.cs` - Level state tracking
- `AdaptiveController.cs` - Feedback controller and routing
- `LoadBalancerConfig.cs` - Runtime configuration
- `LoadBalancerService.cs` - **COMPLETELY REWRITTEN** with adaptive algorithm

### Modified Files
- `Controllers/LoadBalancerController.cs` - **UPDATED** with 7 new API endpoints

### Documentation
- `ADAPTIVE_LOAD_BALANCER.md` - Comprehensive documentation
- `test-adaptive-load-balancer.sh` - Test script

## Quick API Reference

### Main Endpoint (Use This!)
```bash
# Assign a pilgrim to a level
curl -X POST http://localhost:5000/api/LoadBalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 68, "isDisabled": false}'
```

### Other Key Endpoints
```bash
# Get metrics
curl -X GET http://localhost:5000/api/LoadBalancer/metrics

# Update level states (feed real-time congestion data)
curl -X POST http://localhost:5000/api/LoadBalancer/levels/state \
  -H "Content-Type: application/json" \
  -d '{"levels":[{"level":1,"waitEst":13.5}]}'

# Update configuration
curl -X POST http://localhost:5000/api/LoadBalancer/config \
  -H "Content-Type: application/json" \
  -d '{"alpha1": 0.4, "waitTargetMinutes": 10}'

# Health check
curl -X GET http://localhost:5000/api/LoadBalancer/health
```

## How It Works (In Plain English)

1. **Disabled pilgrims always go to Level 1** (highest priority)

2. **For elderly non-disabled pilgrims:**
   - System computes a dynamic "age cutoff" based on recent arrivals
   - If age ≥ cutoff → Level 1
   - If age < cutoff → Less busy of Level 2 or 3
   - The cutoff adapts: more disabled arrivals → higher cutoff (fewer elderly fit)

3. **Feedback controller maintains Level 1 performance:**
   - Monitors Level 1 wait time every minute
   - If too slow → reduce target share (send fewer people)
   - If fast enough → increase target share (send more people)
   - Automatically adjusts to changing conditions

4. **No fixed thresholds anywhere** - everything adapts to current data

## Key Differences from Old System

| Old System | New System |
|------------|------------|
| Fixed age 60+ threshold | Dynamic age cutoff |
| `isHealthy` (boolean) | `isDisabled` (inverted) |
| Simple endpoint `/assign` | Rich endpoint `/arrivals/assign` |
| Capacity-based | Wait-time based |
| Static | Adaptive with feedback |

## Configuration Defaults

```
alpha1: 0.35              (target 35% to Level 1)
alpha1Min: 0.15           (minimum 15%)
alpha1Max: 0.55           (maximum 55%)
waitTargetMinutes: 12     (keep Level 1 around 12 min wait)
controllerGain: 0.03      (feedback gain)
slidingWindowMinutes: 45  (look back 45 minutes)
softGateEnabled: true     (prevent overshooting)
randomizationEnabled: true (reduce oscillations)
```

## Testing

Run the test script:
```bash
./test-adaptive-load-balancer.sh
```

This will:
- Check health
- Assign disabled pilgrims (verify they go to Level 1)
- Assign non-disabled pilgrims of various ages
- Update level states
- Trigger controller tick
- Check metrics
- Update configuration
- Test edge cases

## Monitoring

Use the `/metrics` endpoint to monitor:
- Current `alpha1` and `ageCutoff`
- Fraction of disabled arrivals (`pDisabled`)
- Total arrivals in rolling window
- Age quantiles (50th, 80th, 90th percentiles)
- Wait estimates for all levels

## Backward Compatibility

Old endpoints still work:
- `POST /api/LoadBalancer/assign` (maps `isHealthy` to `isDisabled`)
- `GET /api/LoadBalancer/utilization`
- `POST /api/LoadBalancer/reset`

But they have limited functionality. **Use the new endpoints for full features.**

## Next Steps

1. **Start the service** (if not already running)
2. **Run the test script** to verify everything works
3. **Monitor metrics** via `/metrics` endpoint
4. **Feed real-time data** via `/levels/state` endpoint
5. **Tune configuration** via `/config` endpoint if needed

## Need Help?

- See `ADAPTIVE_LOAD_BALANCER.md` for full documentation
- Check API responses - they include detailed decision reasoning
- Use `traceId` in responses for debugging specific assignments
- Monitor `/health` endpoint for service status

## Mathematical Foundations

The controller implements this update rule every minute:

```
error = target_wait - actual_wait_level_1
alpha1_new = alpha1 + gain * error
alpha1_new = clip(alpha1_new, max(min, p_disabled), max)

share_for_old = alpha1 - p_disabled
tau = 1 - share_for_old
age_cutoff = quantile(tau) of non-disabled ages
```

This ensures:
- Level 1 maintains target wait time
- Disabled always have enough capacity
- Age cutoff adapts to arrival patterns
- System is self-correcting

---

**Ready to use!** The service is fully implemented and backward compatible. 🚀
