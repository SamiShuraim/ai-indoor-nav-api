# Load Balancer Implementation

## Overview
This implementation provides a load balancing system that assigns users to different levels (floors) based on their age, health condition, and current utilization levels.

## API Endpoints

### 1. Assign Level
**Endpoint:** `POST /api/LoadBalancer/assign`

**Request Body:**
```json
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

### 2. Get Utilization
**Endpoint:** `GET /api/LoadBalancer/utilization`

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
    "2": {
      "level": 2,
      "currentUtilization": 1200,
      "capacity": 3000,
      "utilizationPercentage": 40.0
    },
    "3": {
      "level": 3,
      "currentUtilization": 1100,
      "capacity": 3000,
      "utilizationPercentage": 36.67
    }
  }
}
```

### 3. Reset Utilization (Optional)
**Endpoint:** `POST /api/LoadBalancer/reset`

**Response:**
```json
{
  "message": "Utilization counters have been reset"
}
```

## Assignment Logic

### Level Capacities
- **Level 1:** 500 people (premium level for elderly/unhealthy)
- **Level 2:** 3000 people
- **Level 3:** 3000 people

### Priority Score Calculation
Each user receives a priority score (0-100) based on:
- **Age Component (0-70 points):** Age × 0.7 (capped at 70)
- **Health Component (0-30 points):** Unhealthy adds 30 points

Examples:
- 70 years old, unhealthy: 49 + 30 = **79 points** (high priority)
- 60 years old, unhealthy: 42 + 30 = **72 points** (high priority)
- 30 years old, healthy: 21 + 0 = **21 points** (low priority)
- 80 years old, healthy: 56 + 0 = **56 points** (medium-high priority)

### Assignment Rules

1. **Low Utilization Phase (Level 1 < 60% full):**
   - Everyone goes to Level 1 unless it's at capacity
   - Rationale: Why send someone to Level 3 when Level 1 has space?

2. **Moderate Utilization Phase (Level 1 = 60-80% full):**
   - High priority (score ≥ 60): Assigned to Level 1
   - Medium priority (score 40-59): Assigned to Level 1 if < 80% full, otherwise Level 2/3
   - Low priority (score < 40): Assigned to Level 2 or 3

3. **High Utilization Phase (Level 1 > 80% full):**
   - Only high priority users (score ≥ 60) get Level 1
   - All others go to Level 2 or 3
   - Level 2 and 3 are balanced to distribute load evenly

4. **Overflow Handling:**
   - If all levels are full, high priority users still get Level 1
   - Others overflow to the least utilized level (2 or 3)

## Example Scenarios

### Scenario 1: Early Morning (Low Traffic)
- **State:** Level 1: 50/500 (10%), Level 2: 0/3000, Level 3: 0/3000
- **Request:** 25-year-old, healthy (priority score: 17)
- **Result:** Assigned to Level 1 (still under 60% threshold)

### Scenario 2: Peak Hours (Medium Traffic)
- **State:** Level 1: 350/500 (70%), Level 2: 800/3000, Level 3: 750/3000
- **Request 1:** 70-year-old, unhealthy (priority score: 79)
- **Result 1:** Assigned to Level 1 (high priority)
- **Request 2:** 25-year-old, healthy (priority score: 17)
- **Result 2:** Assigned to Level 2 (low priority, Level 1 selective)

### Scenario 3: Near Capacity
- **State:** Level 1: 490/500 (98%), Level 2: 2800/3000, Level 3: 2700/3000
- **Request 1:** 80-year-old, unhealthy (priority score: 86)
- **Result 1:** Assigned to Level 1 (high priority, last spots reserved)
- **Request 2:** 35-year-old, healthy (priority score: 24)
- **Result 2:** Assigned to Level 3 (lower utilization than Level 2)

## Implementation Details

### Thread Safety
- Uses `ConcurrentDictionary` for thread-safe utilization tracking
- Singleton service registration ensures shared state across all requests

### Service Registration
```csharp
builder.Services.AddSingleton<LoadBalancerService>();
```

### Components Created
1. **Models:**
   - `LevelAssignmentRequest.cs` - Input model for age and health
   - `LevelAssignmentResponse.cs` - Response with assignment details
   - `LevelUtilizationResponse.cs` - Current utilization info

2. **Service:**
   - `LoadBalancerService.cs` - Core logic for level assignment

3. **Controller:**
   - `LoadBalancerController.cs` - API endpoints

## Testing the API

### Using curl:

```bash
# Assign a level
curl -X POST http://localhost:5000/api/LoadBalancer/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 65, "isHealthy": false}'

# Get current utilization
curl http://localhost:5000/api/LoadBalancer/utilization

# Reset utilization (for testing)
curl -X POST http://localhost:5000/api/LoadBalancer/reset
```

### Using Swagger UI:
Navigate to `http://localhost:5000/swagger` to access the interactive API documentation.

## Future Enhancements

Possible improvements:
1. Persist utilization to database for crash recovery
2. Add time-based auto-reset (e.g., daily at midnight)
3. Add authentication/authorization
4. Add analytics and reporting endpoints
5. Implement machine learning for dynamic threshold adjustment
6. Add webhook notifications for capacity alerts
