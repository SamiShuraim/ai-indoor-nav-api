# Mobile App & Admin Dashboard Integration Guide

## Overview

This document outlines the expected behavior and API interactions for both the **Mobile Application** and **Admin Dashboard** when integrating with the Adaptive Load Balancer system.

---

## 📱 Mobile Application

### Responsibility
The mobile app's **ONLY** responsibility is to request a level assignment for each arriving pilgrim and display the result.

### Required Integration

#### 1. Assign Pilgrim to Level

**Endpoint:** `POST /api/LoadBalancer/arrivals/assign`

**When to call:** When a pilgrim arrives and needs to be assigned to a level (Level 1, 2, or 3).

**Request:**
```json
{
  "age": 45,
  "isDisabled": false
}
```

**Request Fields:**
- `age` (integer, required): Pilgrim's age in years (0-120)
- `isDisabled` (boolean, required): Whether the pilgrim has a disability

**Response (Success - 200 OK):**
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

**Response Fields:**
- `level` (integer): **The assigned level (1, 2, or 3)** - **THIS IS WHAT YOU DISPLAY TO THE USER**
- `decision` (object): Detailed decision information (optional to display)
  - `reason` (string): Human-readable explanation for the assignment
  - `waitEst` (object): Current estimated wait times at each level (in minutes)
  - Other fields are for internal tracking/debugging
- `traceId` (string): Unique identifier for this assignment (useful for support/debugging)

**Response (Error - 400 Bad Request):**
```json
{
  "error": "Age must be between 0 and 120, got 150"
}
```

**Response (Error - 500 Internal Server Error):**
```json
{
  "error": "An error occurred while assigning level: [error details]"
}
```

### User Flow Example

```
1. Pilgrim approaches entry point
2. Staff/Pilgrim enters age and disability status in mobile app
3. App calls POST /arrivals/assign
4. App receives response with assigned level
5. App displays: "Please proceed to Level 2"
   (Optional: Also show estimated wait time from decision.waitEst)
6. Pilgrim goes to their assigned level
7. DONE - No further action required!
```

### What the Mobile App Does NOT Need to Do

❌ Track when pilgrim arrives at the level  
❌ Track when pilgrim leaves the level  
❌ Update queue information  
❌ Report congestion data  
❌ Call any other endpoints  

**The backend handles all tracking automatically!**

### Example Mobile App Implementation (Pseudocode)

```javascript
async function assignPilgrim(age, isDisabled) {
  try {
    const response = await fetch('http://api.example.com/api/LoadBalancer/arrivals/assign', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ age, isDisabled })
    });

    if (!response.ok) {
      const error = await response.json();
      showError(error.error);
      return;
    }

    const data = await response.json();
    
    // Display assigned level to user
    showAssignment({
      level: data.level,
      estimatedWait: data.decision.waitEst[data.level],
      reason: data.decision.reason
    });

    // Optionally log traceId for support
    console.log('Assignment ID:', data.traceId);
    
  } catch (error) {
    showError('Network error: Unable to assign level');
  }
}
```

---

## 📊 Admin Dashboard

### Responsibility
The admin dashboard monitors the real-time state of the system, displays statistics, and allows configuration updates.

### Required Integration

#### 1. Health Check (Optional)

**Endpoint:** `GET /api/LoadBalancer/health`

**When to call:** On dashboard load to verify the service is running.

**Response (200 OK):**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-29T15:30:45.123Z"
}
```

---

#### 2. Get Real-Time Metrics (Primary Dashboard View)

**Endpoint:** `GET /api/LoadBalancer/metrics`

**When to call:** Poll every 10-30 seconds to display real-time statistics.

**Response (200 OK):**
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

**Key Metrics to Display:**

**Controller Status:**
- `alpha1`: Current target fraction for Level 1 (0-1)
- `ageCutoff`: Current dynamic age cutoff (pilgrims >= this age go to Level 1)
- `pDisabled`: Proportion of disabled pilgrims (0-1)

**Arrival Statistics (Rolling Window - Last 45 Minutes):**
- `counts.total`: Total arrivals
- `counts.disabled`: Disabled pilgrims
- `counts.nonDisabled`: Non-disabled pilgrims
- `quantilesNonDisabledAge`: Age distribution (50th, 80th, 90th percentiles)

**Level Status (Real-Time):**
For each level (1, 2, 3):
- `queueLength`: Number of pilgrims currently at the level
- `waitEst`: Estimated wait time in minutes
- `throughputPerMin`: Processing rate (pilgrims per minute)

**Configuration:**
- `waitTargetMinutes`: Target wait time for Level 1
- `alpha1Min`, `alpha1Max`: Bounds for alpha1 adjustment
- `controllerGain`: Controller responsiveness

---

#### 3. Get Current Configuration

**Endpoint:** `GET /api/LoadBalancer/config`

**When to call:** On dashboard load or when viewing configuration settings.

**Response (200 OK):**
```json
{
  "alpha1": 0.35,
  "alpha1Min": 0.15,
  "alpha1Max": 0.55,
  "waitTargetMinutes": 12.0,
  "controllerGain": 0.03,
  "window": {
    "mode": "sliding",
    "minutes": 45.0,
    "halfLifeMinutes": 45.0
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

---

#### 4. Update Configuration (Optional)

**Endpoint:** `POST /api/LoadBalancer/config`

**When to call:** When admin wants to adjust system parameters.

**Request (all fields optional):**
```json
{
  "alpha1": 0.4,
  "waitTargetMinutes": 10.0
}
```

**Available Configuration Fields:**
- `alpha1`: Target fraction for Level 1 (0-1)
- `alpha1Min`: Minimum allowed alpha1 (0-1)
- `alpha1Max`: Maximum allowed alpha1 (0-1)
- `waitTargetMinutes`: Target wait time for Level 1 (minutes)
- `controllerGain`: Controller responsiveness (0.01-0.10 typical)
- `window`: Statistical window configuration
  - `mode`: "sliding" or "decay"
  - `minutes`: Window size (default 45)
  - `halfLifeMinutes`: Decay rate if mode is "decay"
- `softGate`: Prevent overshooting alpha1
  - `enabled`: true/false
  - `bandYears`: Age band width (default 3.0)
- `randomization`: Add randomness in boundary band
  - `enabled`: true/false
  - `rate`: Randomization rate (0-1, default 0.07)

**Response (200 OK):**
Returns the full updated configuration (same format as GET /config).

---

#### 5. Manual Controller Tick (Testing Only)

**Endpoint:** `POST /api/LoadBalancer/control/tick`

**When to call:** Only for testing/debugging. The controller runs automatically every minute.

**Response (200 OK):**
```json
{
  "alpha1": 0.35,
  "ageCutoff": 42.5,
  "pDisabled": 0.12,
  "window": {
    "method": "sliding",
    "slidingWindowMinutes": 45.0,
    "halfLifeMin": null
  }
}
```

---

### Dashboard UI Suggestions

#### Real-Time Overview Panel
```
╔══════════════════════════════════════════════════════════╗
║  ADAPTIVE LOAD BALANCER - REAL-TIME STATUS              ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║  Controller Status:                                       ║
║  • Alpha1 (Level 1 Target): 35%                          ║
║  • Age Cutoff: 42.5 years                                ║
║  • Disabled Proportion: 12%                              ║
║                                                           ║
║  Arrivals (Last 45 Minutes):                             ║
║  • Total: 150 pilgrims                                   ║
║  • Disabled: 18 | Non-Disabled: 132                      ║
║  • Age Distribution: 50%=38yrs, 80%=55yrs, 90%=68yrs    ║
║                                                           ║
╚══════════════════════════════════════════════════════════╝
```

#### Level Status Cards
```
╔═══════════════╗  ╔═══════════════╗  ╔═══════════════╗
║   LEVEL 1     ║  ║   LEVEL 2     ║  ║   LEVEL 3     ║
╠═══════════════╣  ╠═══════════════╣  ╠═══════════════╣
║ Queue: 45     ║  ║ Queue: 52     ║  ║ Queue: 48     ║
║ Wait: 13.2min ║  ║ Wait: 11.5min ║  ║ Wait: 12.8min ║
║ Rate: 3.4/min ║  ║ Rate: 4.5/min ║  ║ Rate: 3.8/min ║
╚═══════════════╝  ╚═══════════════╝  ╚═══════════════╝
```

#### Configuration Panel (Optional)
```
╔══════════════════════════════════════════════════════════╗
║  CONFIGURATION                                            ║
╠══════════════════════════════════════════════════════════╣
║                                                           ║
║  Wait Target: [12.0] minutes                             ║
║  Alpha1 Range: [0.15] to [0.55]                          ║
║  Controller Gain: [0.03]                                 ║
║                                                           ║
║  [Update Configuration]                                   ║
║                                                           ║
╚══════════════════════════════════════════════════════════╝
```

### Example Dashboard Implementation (Pseudocode)

```javascript
// Poll metrics every 15 seconds
setInterval(async () => {
  try {
    const response = await fetch('http://api.example.com/api/LoadBalancer/metrics');
    const metrics = await response.json();
    
    // Update controller status
    updateControllerStatus({
      alpha1: metrics.alpha1,
      ageCutoff: metrics.ageCutoff,
      pDisabled: metrics.pDisabled
    });
    
    // Update arrival statistics
    updateArrivalStats({
      total: metrics.counts.total,
      disabled: metrics.counts.disabled,
      nonDisabled: metrics.counts.nonDisabled,
      ageQuantiles: metrics.quantilesNonDisabledAge
    });
    
    // Update level cards
    for (const [level, data] of Object.entries(metrics.levels)) {
      updateLevelCard(level, {
        queueLength: data.queueLength,
        waitEst: data.waitEst,
        throughput: data.throughputPerMin
      });
    }
    
  } catch (error) {
    showError('Unable to fetch metrics');
  }
}, 15000); // Poll every 15 seconds
```

---

## 🔄 System Flow Diagram

```
┌─────────────────┐
│  Mobile App     │
│  (Assignment)   │
└────────┬────────┘
         │
         │ POST /arrivals/assign
         │ {age: 45, isDisabled: false}
         │
         ▼
┌─────────────────────────────────────────┐
│  Load Balancer Service                  │
│  ┌─────────────────────────────────┐   │
│  │ 1. Validate age                 │   │
│  │ 2. Record in rolling stats      │   │
│  │ 3. Route to level               │   │
│  │ 4. Track arrival (45min timer)  │◄──┼─── Automatic every minute
│  │ 5. Return assignment             │   │     - Update level states
│  └─────────────────────────────────┘   │     - Adjust alpha1
│                                         │     - Compute age cutoff
└────────┬────────────────────────────────┘
         │
         │ Response: {level: 2, ...}
         │
         ▼
┌─────────────────┐
│  Mobile App     │
│  Shows Level 2  │
└─────────────────┘

         ┌─────────────────────────────────┐
         │  Admin Dashboard                │
         │  (Monitoring)                   │
         └────────┬────────────────────────┘
                  │
                  │ GET /metrics (every 15 seconds)
                  │
                  ▼
         ┌─────────────────────────────────┐
         │  Displays:                      │
         │  - Current queue lengths        │
         │  - Wait times                   │
         │  - Throughput rates             │
         │  - Controller status            │
         │  - Arrival statistics           │
         └─────────────────────────────────┘
```

---

## ⚡ Quick Reference

### Mobile App - Single API Call
```
POST /api/LoadBalancer/arrivals/assign
→ Display result.level to user
→ Done!
```

### Admin Dashboard - Polling
```
GET /api/LoadBalancer/metrics (every 15-30 seconds)
→ Display all metrics
→ Update UI in real-time
```

### No Manual Tracking Required!
The system automatically:
- ✅ Tracks arrivals
- ✅ Manages 45-minute pilgrim lifecycle
- ✅ Calculates queue lengths
- ✅ Computes wait times
- ✅ Adjusts routing algorithm
- ✅ Provides real-time statistics

---

## 🎯 Summary

**Mobile Application:**
- **ONE API CALL**: Assign pilgrim to level
- **ONE DISPLAY**: Show assigned level number
- **ZERO tracking responsibilities**

**Admin Dashboard:**
- **POLL**: Get metrics every 15-30 seconds
- **DISPLAY**: Real-time system status
- **CONFIGURE**: (Optional) Adjust system parameters

**Backend:**
- **AUTOMATIC**: Handles all tracking, calculations, and adjustments
- **AUTONOMOUS**: No manual intervention required
- **ADAPTIVE**: Continuously optimizes load distribution
