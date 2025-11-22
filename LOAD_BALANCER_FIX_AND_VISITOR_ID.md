# Load Balancer Fix & Visitor ID System

## Problem 1: Load Balancer Algorithm Not Working

### The Issue
With 2.5 arrivals/second, 5000 L1 capacity, 30000 L2/L3 capacity, and ~19% disability rate, the age cutoff was incorrectly set to 100 (maximum age). The algorithm was stupidly reserving L1 only for disabled people even though there was plenty of space.

### Root Cause
- **TargetAlpha1 was only 7.69%** (originally designed for 500/6500 total capacity)
- With **p(disabled) = 19%**, the calculation was:
  - `shareLeftForOld = max(0, 0.0769 - 0.19) = 0`
  - `tau = 1.0 - 0 = 1.0` (asking for 100th percentile)
  - The 100th percentile is the maximum age seen (100 years)
- The algorithm wasn't capacity-aware at all!

### The Fix

#### 1. Updated `LoadBalancerConfig.cs`
```csharp
// OLD VALUES - TOO CONSERVATIVE
public double TargetAlpha1 { get; set; } = 0.0769; // 7.69%
public double Alpha1Min { get; set; } = 0.05;     // 5%
public double Alpha1Max { get; set; } = 0.12;     // 12%

// NEW VALUES - CAPACITY-AWARE
public double TargetAlpha1 { get; set; } = 0.35;  // 35%
public double Alpha1Min { get; set; } = 0.20;     // 20%
public double Alpha1Max { get; set; } = 0.50;     // 50%
```

**Why these values?**
- With 6,750 total active people (2.5/sec × 45min):
  - 19% disabled = ~1,280 people
  - L1 capacity = 5,000 people
  - Plenty of room for ~3,720 elderly people!
- 35% target means ~2,363 people to L1 (1,280 disabled + 1,083 elderly)
- This leaves room for growth while maintaining 90% utilization target

#### 2. Made Controller Tick Capacity-Aware
Added intelligent capacity monitoring in `LoadBalancerService.cs`:

```csharp
// Capacity-aware adjustment
double capacityAvailable = _config.L1CapSoft - active1;
double capacityRatio = capacityAvailable / _config.L1CapSoft;

// If we have lots of capacity (>30% free) and low utilization (<70%), boost alpha1
if (capacityRatio > 0.30 && util < 0.70)
{
    double targetAlpha1 = Math.Min(_config.Alpha1Max, pDisabled + 0.20);
    _alpha1 = _alpha1 + 0.1 * (targetAlpha1 - _alpha1); // Fast adjustment
}
```

**Key improvements:**
- **Always reserves at least 5% for elderly:** `lowerBound = max(Alpha1Min, pDisabled + 0.05)`
- **Prevents tau=1.0 issue:** Falls back to age 70 cutoff if shareLeftForOld < 0.01
- **Fast capacity utilization:** Aggressively increases alpha1 when L1 has >30% free space

### Expected Behavior Now
- With p(disabled) = 19%, alpha1 will be at least 24% (19% + 5%)
- This gives `shareLeftForOld = 0.24 - 0.19 = 0.05` (5% for elderly)
- `tau = 1 - 0.05 = 0.95` (95th percentile age)
- Age cutoff will be ~75-80 years instead of 100
- As L1 fills up less, alpha1 will increase to 35-50%, giving even more room

---

## Problem 2: Unique Visitor IDs for QR Code Scanning

### Requirements
Every visitor should have a unique ID that can be:
1. Displayed in the app
2. Scanned via QR code
3. Shows their age, status, and assigned level

### Implementation

#### New Files Created

##### 1. `Models/Visitor.cs`
- `Visitor`: Stores visitor information
- `VisitorInfoResponse`: API response format

##### 2. `Services/VisitorService.cs`
- Generates unique 8-character IDs (format: `XXXX-XXXX`)
- Uses readable characters only (excludes I/1, O/0 for clarity)
- Automatically cleans up expired visitors (1 hour after expiry)
- Thread-safe with proper locking

##### 3. `Controllers/VisitorController.cs`
Three endpoints:
- `GET /api/visitor/{id}` - Returns JSON visitor info
- `GET /api/visitor/{id}/page` - Returns beautiful HTML page for QR scanning
- `GET /api/visitor/count` - Returns active visitor count

#### Updated Files

##### 1. `Models/ArrivalAssignResponse.cs`
Added `VisitorId` field to response:
```csharp
public string VisitorId { get; set; } = string.Empty;
```

##### 2. `Services/LoadBalancerService.cs`
- Now accepts `VisitorService` via dependency injection
- Creates visitor record on each assignment
- Returns visitor ID in response

##### 3. `Program.cs`
Registered `VisitorService` as singleton:
```csharp
builder.Services.AddSingleton<VisitorService>();
```

### How to Use

#### 1. Assign a visitor and get their ID
```bash
POST /api/loadbalancer/arrivals/assign
{
  "age": 75,
  "isDisabled": false
}

Response:
{
  "level": 1,
  "visitorId": "A7K9-M2P4",
  "decision": { ... },
  "traceId": "..."
}
```

#### 2. Look up visitor by ID (API)
```bash
GET /api/visitor/A7K9-M2P4

Response:
{
  "visitorId": "A7K9-M2P4",
  "age": 75,
  "status": "Non-Disabled",
  "assignedLevel": 1,
  "assignedAt": "2025-11-22T10:30:00Z",
  "expiresAt": "2025-11-22T11:15:00Z",
  "isExpired": false
}
```

#### 3. QR Code Scanning (Beautiful HTML Page)
```bash
GET /api/visitor/A7K9-M2P4/page
```

This returns a beautiful, responsive HTML page showing:
- **Visitor ID** in large, readable format
- **Active/Expired badge** with color coding
- **Age** prominently displayed
- **Status** (Disabled/Non-Disabled) with visual indicator
- **Assigned Level** in a large colored box (L1=green, L2=orange, L3=purple)
- **Timestamps** for when assignment was made and when it expires

The page is:
- Mobile-responsive
- Beautiful gradient background
- Clear, readable fonts
- Color-coded by status and level
- Perfect for QR code scanning

### Integration with Apps

#### Mobile App Flow
1. **User gets assigned:** Call `/api/loadbalancer/arrivals/assign`
2. **Show visitor ID:** Display `visitorId` in the app UI
3. **Generate QR code:** Encode URL `https://your-api.com/api/visitor/{visitorId}/page`
4. **User can click button:** Opens web page showing their info
5. **Staff can scan:** QR code takes them to the same page

#### Example QR Code Content
```
https://your-api.com/api/visitor/A7K9-M2P4/page
```

## Testing the Changes

### Test Load Balancer Fix
```bash
# Send 100 arrivals with 19% disabled
for i in {1..100}; do
  if [ $((i % 5)) -eq 0 ]; then
    curl -X POST http://localhost:5000/api/loadbalancer/arrivals/assign \
      -H "Content-Type: application/json" \
      -d '{"age": 50, "isDisabled": true}'
  else
    age=$((RANDOM % 60 + 20))
    curl -X POST http://localhost:5000/api/loadbalancer/arrivals/assign \
      -H "Content-Type: application/json" \
      -d "{\"age\": $age, \"isDisabled\": false}"
  fi
done

# Check metrics - age cutoff should be reasonable now (not 100!)
curl http://localhost:5000/api/loadbalancer/metrics
```

### Test Visitor ID System
```bash
# 1. Create a visitor
RESPONSE=$(curl -X POST http://localhost:5000/api/loadbalancer/arrivals/assign \
  -H "Content-Type: application/json" \
  -d '{"age": 75, "isDisabled": false}')

# Extract visitor ID
VISITOR_ID=$(echo $RESPONSE | jq -r '.visitorId')
echo "Visitor ID: $VISITOR_ID"

# 2. Look up visitor info (JSON)
curl http://localhost:5000/api/visitor/$VISITOR_ID

# 3. Open visitor page in browser (beautiful HTML)
open http://localhost:5000/api/visitor/$VISITOR_ID/page
```

## Summary of Changes

### Files Modified
1. ✅ `Services/LoadBalancerConfig.cs` - Updated alpha1 targets (35% instead of 7.69%)
2. ✅ `Services/LoadBalancerService.cs` - Made capacity-aware, added visitor tracking
3. ✅ `Models/ArrivalAssignResponse.cs` - Added VisitorId field
4. ✅ `Program.cs` - Registered VisitorService

### Files Created
1. ✅ `Models/Visitor.cs` - Visitor data models
2. ✅ `Services/VisitorService.cs` - Visitor ID generation and management
3. ✅ `Controllers/VisitorController.cs` - API endpoints for visitor lookup

### API Changes
- **Modified:** `/api/loadbalancer/arrivals/assign` now returns `visitorId`
- **New:** `/api/visitor/{id}` - Get visitor info as JSON
- **New:** `/api/visitor/{id}/page` - Get visitor info as beautiful HTML
- **New:** `/api/visitor/count` - Get active visitor count

## Result

### Load Balancer
- ✅ No longer stuck at age 100 cutoff
- ✅ Uses L1 capacity intelligently
- ✅ Reserves appropriate space for both disabled and elderly
- ✅ Adapts to actual disability rates
- ✅ Age cutoff will be around 75-80 years with 19% disabled rate

### Visitor IDs
- ✅ Every visitor gets unique, scannable ID
- ✅ IDs are short and readable (XXXX-XXXX format)
- ✅ Beautiful web page for QR code scanning
- ✅ Shows age, status, and assigned level
- ✅ Automatic cleanup of expired records
- ✅ Mobile-responsive design
