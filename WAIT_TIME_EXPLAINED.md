# Wait Time, Queue Length, and Throughput - Explained

## The Physical Setup (What's Actually Happening)

### Scenario:
```
Entry Point → Pilgrim gets assigned → Goes to Level 1, 2, or 3 → Waits in line → Gets processed → Leaves
```

### At Each Level:

```
┌─────────────────────────────────────────────────┐
│  Level 1 (Example: Prayer Area)                │
│                                                 │
│  Entrance                                       │
│     ↓                                           │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐      │
│  │  🧑  │ │  🧑  │ │  🧑  │ │  🧑  │ │  🧑  │  ← QUEUE (Waiting)
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘      │
│                                                 │
│  ⬇️  Being Served (Praying, Viewing, etc.)      │
│                                                 │
│  Exit ➡️  (Pilgrim leaves after ~45 min)        │
└─────────────────────────────────────────────────┘
```

---

## Key Terms Explained

### 1. **Queue Length** (Number of People)
**What it is:** How many pilgrims are currently at that level.

**Example:**
- Level 1 has 45 pilgrims
- Level 2 has 52 pilgrims  
- Level 3 has 48 pilgrims

**Why it matters:** More people = more crowded = longer waits

---

### 2. **Throughput** (People per Minute)
**What it is:** How many pilgrims leave/complete their visit per minute.

**Example:**
- Level 1 processes 3.4 pilgrims/minute
- Level 2 processes 4.5 pilgrims/minute
- Level 3 processes 3.8 pilgrims/minute

**Why it matters:** Higher throughput = faster processing = shorter waits

**What affects it:**
- Physical space size
- Activity duration (if it's guided)
- Staff efficiency
- Crowd management

---

### 3. **Wait Time** (Minutes)
**What it is:** How long a newly arriving pilgrim has to wait before they can be served/processed.

**Formula:** `Wait Time = Queue Length / Throughput`

**Example for Level 1:**
- Queue Length: 45 people
- Throughput: 3.4 people/minute
- Wait Time: 45 / 3.4 = **13.2 minutes**

**Meaning:** If you arrive now at Level 1, you'll wait about 13 minutes before you can start your activity.

---

## Real-World Example

### Let's say Level 1 is a prayer area:

**At 10:00 AM:**
- 45 pilgrims are inside praying
- On average, 3.4 pilgrims finish and leave per minute
- A new pilgrim arrives

**Question:** How long until this new pilgrim can start praying?

**Answer:** About 13 minutes (45 ÷ 3.4)

**Why?**
- Current 45 people will take 45 ÷ 3.4 = 13.2 minutes to process
- Then the new pilgrim can enter

---

## What the System is Trying to Do

### Problem:
Different levels have different capacities and processing speeds. Without smart assignment, one level could get overwhelmed while others are empty.

### Goal:
**Balance the load** so all levels have reasonable wait times.

### The Target:
- Keep Level 1 wait time around **12 minutes**
- Keep all levels reasonably balanced
- Prioritize disabled and older pilgrims for Level 1 (easier access)

---

## How Queue Length Changes

### Arrivals Increase Queue:
```
Time 10:00: Queue = 45
Pilgrim A arrives and is assigned to Level 1
Time 10:01: Queue = 46
```

### Throughput Decreases Queue:
```
Time 10:00: Queue = 45, Throughput = 3.4/min
After 1 minute: ~3-4 pilgrims leave
Time 10:01: Queue = 41-42
```

### In our simplified system:
Since pilgrims stay for exactly **45 minutes**, after assignment:
- They arrive immediately (queue +1)
- They leave after 45 minutes (queue -1)

---

## Why Wait Time Matters

### Good Wait Times (< 15 minutes):
- ✅ Pilgrims are happy
- ✅ Smooth flow
- ✅ No overcrowding

### Bad Wait Times (> 30 minutes):
- ❌ Pilgrims frustrated
- ❌ Crowding and safety issues
- ❌ Poor experience

---

## The Assignment Decision

When a pilgrim arrives, the system looks at wait times:

**Example Scenario:**
```
Level 1: Wait = 13 minutes (45 people, 3.4/min)
Level 2: Wait = 11 minutes (52 people, 4.5/min)
Level 3: Wait = 12 minutes (48 people, 3.8/min)
```

**Decision Logic:**
- Disabled pilgrim → Level 1 (always, regardless of wait)
- Older pilgrim (age ≥ cutoff) → Level 1 (preferred)
- Younger pilgrim → Level 2 (currently lowest wait)

---

## Your Actual Question: What Are We Measuring?

### Physical Reality:
1. **Queue Length** = Number of pilgrims physically at that level
2. **Throughput** = How fast people are processed/leave
3. **Wait Time** = How long you wait before being served

### In Your System:
- Mobile app assigns pilgrim to a level
- Pilgrim goes there (assumed immediate arrival)
- Pilgrim stays for 45 minutes
- System tracks how many are currently there
- System estimates wait times to make better assignments

---

## Do You Actually Need to Track This?

### Question: What happens at each level?
1. **Is it a fixed-time activity?** (e.g., 45-minute guided tour)
   - Everyone takes same time
   - Wait time doesn't really matter
   - You just need to **balance capacity**

2. **Is it a queue for service?** (e.g., security check, ticket counter)
   - Service time varies
   - Wait time REALLY matters
   - You need to **track and balance wait times**

3. **Is it a free-roam area?** (e.g., viewing platform, garden)
   - No strict queue
   - Just avoid overcrowding
   - You just need to **track occupancy limits**

---

## Simplified Understanding

**Simple version:**
- **Queue = How many people are there**
- **Wait = How long you'll wait**
- **Throughput = How fast people leave**

**Why we care:**
We want to send pilgrims to levels that aren't too crowded, so they don't wait forever.

---

## Question for You

**What actually happens at each level in your real scenario?**

A. Fixed-time activity (everyone spends same time)  
B. Queue for service (like security, ticketing)  
C. Free-roam area (viewing platform, garden)  
D. Something else?

This will help me understand if you even need all this complexity!
