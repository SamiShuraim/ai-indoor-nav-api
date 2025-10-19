# Load Balancer API - Quick Start

## Implementation Complete ✅

A load balancing system has been implemented to assign users to levels based on age, health condition, and current utilization.

## API Endpoints

### 1. Assign User to Level
```bash
POST /api/LoadBalancer/assign
Content-Type: application/json

{
  "age": 65,
  "isHealthy": false
}
```

**Response:**
```json
{
  "assignedLevel": 1,
  "currentUtilization": 150,
  "capacity": 500,
  "utilizationPercentage": 30.0
}
```

### 2. View Current Utilization
```bash
GET /api/LoadBalancer/utilization
```

**Response:**
```json
{
  "levels": {
    "1": {
      "level": 1,
      "currentUtilization": 150,
      "capacity": 500,
      "utilizationPercentage": 30.0
    },
    "2": { ... },
    "3": { ... }
  }
}
```

### 3. Reset Utilization (Testing)
```bash
POST /api/LoadBalancer/reset
```

## How It Works

### Level Capacities
- **Level 1:** 500 people (for elderly/unhealthy)
- **Level 2:** 3000 people
- **Level 3:** 3000 people

### Assignment Strategy

**Early in the day (Level 1 < 60% full):**
- Everyone goes to Level 1
- Why send someone to Level 3 when Level 1 has space?

**As capacity increases (Level 1 = 60-80% full):**
- High priority users (elderly/unhealthy) → Level 1
- Others → Level 2 or 3

**Near capacity (Level 1 > 80% full):**
- Only elderly/unhealthy → Level 1
- Everyone else → Level 2 or 3 (balanced)

### Priority Calculation
Each user gets a priority score (0-100):
- **Age:** age × 0.7 (max 70 points)
- **Health:** +30 points if unhealthy

Examples:
- 75 years old, unhealthy = 52.5 + 30 = **82.5** (high priority → Level 1)
- 25 years old, healthy = 17.5 + 0 = **17.5** (low priority → Level 2/3)

## Testing Examples

```bash
# Scenario 1: Young, healthy person (early morning)
curl -X POST http://localhost:5000/api/LoadBalancer/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 25, "isHealthy": true}'
# Expected: Level 1 (low utilization)

# Scenario 2: Elderly, unhealthy person (anytime)
curl -X POST http://localhost:5000/api/LoadBalancer/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 75, "isHealthy": false}'
# Expected: Level 1 (high priority)

# Check utilization
curl http://localhost:5000/api/LoadBalancer/utilization
```

## Files Created

1. **Models/**
   - `LevelAssignmentRequest.cs` - Input (age, isHealthy)
   - `LevelAssignmentResponse.cs` - Assignment result
   - `LevelUtilizationResponse.cs` - Current utilization

2. **Services/**
   - `LoadBalancerService.cs` - Core assignment logic

3. **Controllers/**
   - `LoadBalancerController.cs` - API endpoints

4. **Program.cs** - Service registered as singleton

## Implementation Highlights

✅ Thread-safe utilization tracking (ConcurrentDictionary)
✅ Intelligent assignment based on priority scores
✅ Dynamic thresholds based on utilization
✅ Load balancing between Level 2 and 3
✅ Comprehensive error handling
✅ Swagger documentation support

See `LOAD_BALANCER_IMPLEMENTATION.md` for detailed documentation.
