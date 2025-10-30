# Adaptive Load Balancer - Simple Explanation

## The Core Problem

**Goal:** Assign arriving pilgrims to 3 levels, keeping Level 1 wait times around 12 minutes.

**Rules:**
1. Disabled pilgrims ALWAYS go to Level 1 (accessibility)
2. Older non-disabled pilgrims should preferably go to Level 1
3. Younger non-disabled pilgrims go to Level 2 or Level 3 (less busy one)
4. Keep Level 1 at target capacity (~35% of traffic)

---

## Current System Complexity

### Variables Being Tracked:

1. **alpha1** (0.35): Target fraction of pilgrims for Level 1
2. **alpha1Min** (0.15) / **alpha1Max** (0.55): Bounds for alpha1
3. **waitTargetMinutes** (12.0): Desired wait time at Level 1
4. **controllerGain** (0.03): How fast to adjust alpha1
5. **ageCutoff** (dynamic): Age threshold - pilgrims >= this age → Level 1
6. **pDisabled** (dynamic): Current proportion of disabled pilgrims
7. **tau** (dynamic): Quantile for computing age cutoff
8. **windowMinutes** (45): How far back to look at statistics
9. **softGateBandYears** (3.0): Age buffer zone
10. **randomizationRate** (0.07): Randomness in boundary band

### What Happens Every Minute (Automatic Tick):

```
1. Get Level 1 wait time from queue tracker
2. Calculate error = target_wait - actual_wait
3. Adjust alpha1 based on error
4. Calculate how much "space" left for old people = alpha1 - pDisabled
5. Compute age cutoff from quantile estimator
6. Apply soft gate and randomization rules
```

### What Happens on Each Assignment:

```
1. Record arrival statistics
2. If disabled → Level 1 (always)
3. If age >= ageCutoff → Level 1 (maybe, with soft gate check)
4. If age < ageCutoff → Level 2 or 3 (whichever has lower wait)
5. Record arrival in level tracker
6. Start 45-minute countdown timer
```

---

## Why It's Complex

The system is trying to be **adaptive** - it continuously adjusts the age cutoff based on:
- How many disabled people are arriving
- Current congestion at Level 1
- Age distribution of arrivals
- Historical patterns in a 45-minute window

**The problem:** Lots of moving parts that interact with each other!

---

## Simplification Options

### Option 1: Fixed Age Threshold (SIMPLEST)

**Forget all the adaptive stuff. Just use fixed rules:**

```
Rule 1: If disabled → Level 1
Rule 2: If age >= 60 → Level 1
Rule 3: If age < 60 → Level 2 or 3 (less busy one)
```

**Pros:**
- ✅ Super simple
- ✅ Easy to understand and predict
- ✅ No statistics needed
- ✅ Works immediately (no warm-up period)

**Cons:**
- ❌ Doesn't adapt to changing conditions
- ❌ Level 1 might get overloaded or underutilized
- ❌ Fixed 60-year threshold might not be optimal

---

### Option 2: Simple Load Balancing (SIMPLER)

**Remove the feedback controller. Just balance by current queue length:**

```
Rule 1: If disabled → Level 1

Rule 2: If age >= 60:
  - Check if Level 1 queue < threshold (e.g., 50 people)
  - If yes → Level 1
  - If no → Level 2 or 3 (less busy)

Rule 3: If age < 60:
  - Level 2 or 3 (less busy)
```

**Pros:**
- ✅ Relatively simple
- ✅ Protects Level 1 from overload
- ✅ Still prioritizes older pilgrims

**Cons:**
- ❌ Fixed age threshold
- ❌ Fixed queue threshold
- ❌ Doesn't learn from patterns

---

### Option 3: Simplified Adaptive (MIDDLE GROUND)

**Keep the core idea but remove complexity:**

**Simplifications:**
- Remove sliding windows → just count last 100 arrivals
- Remove feedback controller → adjust alpha1 based on simple rules
- Remove soft gate and randomization
- Use fixed percentile (80th) instead of dynamic tau

```
Every 100 arrivals:
  1. Count disabled = D out of 100
  2. Calculate age cutoff = 80th percentile of non-disabled ages
  3. Set alpha1 = 0.35 (fixed, or simple: alpha1 = D + 0.25)

On each assignment:
  1. If disabled → Level 1
  2. If age >= ageCutoff → Level 1
  3. If age < ageCutoff → Level 2 or 3 (less busy)
```

**Pros:**
- ✅ Still adapts to age distribution
- ✅ Much simpler than current system
- ✅ Accounts for disabled proportion
- ✅ Fewer variables

**Cons:**
- ❌ Doesn't actively control Level 1 wait time
- ❌ Might not be as optimal

---

### Option 4: Pure Capacity-Based (VERY SIMPLE)

**Just keep levels balanced by capacity:**

```
On each assignment:
  1. Get current queue at each level
  2. If disabled → Level 1
  3. Otherwise:
     - Capacity score:
       Level 1: queue_length / capacity_weight (weight = 0.35)
       Level 2: queue_length / capacity_weight (weight = 0.33)
       Level 3: queue_length / capacity_weight (weight = 0.32)
     - Assign to level with LOWEST score
```

**Pros:**
- ✅ Extremely simple
- ✅ Always balances load
- ✅ No statistics needed
- ✅ Works immediately

**Cons:**
- ❌ Ignores age completely (except for disabled)
- ❌ Older pilgrims treated same as younger

---

## Current System in One Sentence

**"Dynamically adjust what age counts as 'old' based on arrival patterns and Level 1 congestion, using a feedback loop with a 45-minute historical window and soft boundaries."**

---

## Questions to Help You Decide

1. **Do you NEED the system to adapt automatically?**
   - If yes → Keep adaptive (or use simplified version)
   - If no → Use fixed threshold (much simpler)

2. **Do you NEED to prioritize older pilgrims for Level 1?**
   - If yes → Need age-based routing
   - If no → Just balance by capacity

3. **Do you NEED to control Level 1 wait times?**
   - If yes → Need feedback controller or queue limits
   - If no → Simple rules are fine

4. **How much do conditions change?**
   - Stable crowds → Fixed thresholds work fine
   - Variable crowds → Adaptive is better

5. **What's most important?**
   - **Simplicity** → Option 1 or 4
   - **Balance** → Option 2
   - **Adaptability** → Option 3 (simplified) or current system

---

## My Recommendation

**Start with Option 2: Simple Load Balancing**

```csharp
public int AssignLevel(int age, bool isDisabled)
{
    // Rule 1: Disabled always to Level 1
    if (isDisabled) return 1;
    
    // Get current queues
    var queues = GetQueueLengths(); // {1: 45, 2: 52, 3: 48}
    
    // Rule 2: Age >= 60 prefer Level 1, but cap it
    if (age >= 60)
    {
        if (queues[1] < 60) // Max 60 people at Level 1
            return 1;
        else
            return queues[2] <= queues[3] ? 2 : 3;
    }
    
    // Rule 3: Age < 60 go to less busy of 2/3
    return queues[2] <= queues[3] ? 2 : 3;
}
```

**That's it! No complex variables, no feedback loops, no statistics.**

Then if you need more sophistication later, you can add it incrementally.

---

## What Do You Think?

Tell me:
1. What are you trying to simulate that isn't working?
2. What behavior do you expect vs what you're seeing?
3. Which option above sounds most reasonable?

I can help simplify or even completely rewrite the system to be much simpler!
