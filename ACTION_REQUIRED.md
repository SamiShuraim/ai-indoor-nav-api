# ⚠️ ACTION REQUIRED: Database Schema Fix Needed

## 🚨 Current Status: API Error

Your API is currently failing with:

```
PostgresException (0x80004005): 42703: column r.connected_levels does not exist
```

**Affected Endpoint:** `/api/RouteNode/navigateToLevel`

**Impact:** This endpoint cannot be used until the database schema is updated.

---

## ✅ Solution Available

A complete fix has been prepared for you with multiple options to apply it.

---

## 🎯 IMMEDIATE ACTION (Pick One)

### 🌟 RECOMMENDED: Supabase SQL Editor (5 minutes)

**Best for:** Everyone, no installation needed

1. Open **https://app.supabase.com/**
2. Select your project
3. Go to **SQL Editor** (left sidebar)
4. Open this file: `diagnose_and_fix_migration.sql`
5. Copy and paste the contents into SQL Editor
6. Click **Run**
7. Look for ✓ checkmarks in the output

**Result:** All missing columns will be added safely

---

### 🖥️ ALTERNATIVE: Interactive Script

**Best for:** Users with terminal access and psql installed

```bash
cd /workspace
./quick_fix_script.sh
```

Select option 1 when prompted.

---

### 💻 ALTERNATIVE: Direct psql Command

**Best for:** DevOps/CLI users

```bash
# Replace with your actual database credentials
PGPASSWORD='your_password' psql \
  -h aws-1-ap-southeast-1.pooler.supabase.com \
  -p 6543 \
  -U postgres.xhvapujhplecxkqvepww \
  -d postgres \
  -f diagnose_and_fix_migration.sql
```

---

## 📁 Files Created for You

All files are in `/workspace/`:

### 🔧 Fix Scripts (Ready to Use)

1. **`diagnose_and_fix_migration.sql`** ⭐ RECOMMENDED
   - Complete diagnostic and fix in one script
   - Safe to run (uses IF NOT EXISTS)
   - Shows clear ✓/✗ status for each column
   - Automatically updates migration history

2. **`apply_connection_point_migration.sql`**
   - Simpler version, just adds columns
   - Safe to run (uses IF NOT EXISTS)

3. **`quick_fix_script.sh`**
   - Interactive bash script
   - Auto-loads your .env file
   - Provides menu with options
   - Requires: psql installed, .env file

### 📖 Documentation

4. **`README_COLUMN_FIX.md`** ⭐ START HERE
   - Quick overview
   - Clear step-by-step instructions
   - Links to other resources

5. **`SOLUTION_SUMMARY.md`**
   - Detailed explanation of all solutions
   - Verification steps
   - Prevention strategies

6. **`TROUBLESHOOTING_MIGRATION_ERROR.md`**
   - Deep troubleshooting guide
   - Multiple solution methods
   - Root cause analysis

7. **`FIX_MISSING_COLUMNS.md`**
   - General fix documentation
   - Multiple options explained

---

## 🔍 What's Being Fixed

The following columns will be added to `route_nodes` table:

| Column | Type | Purpose |
|--------|------|---------|
| `is_connection_point` | boolean | Marks nodes that connect levels |
| `connection_type` | varchar(50) | Type: elevator, stairs, ramp |
| `connected_levels` | integer[] | Array of connected level IDs |
| `connection_priority` | integer | Routing preference |

Plus 2 indexes for performance.

---

## ✅ How to Verify Fix

After applying the fix, test with:

```bash
curl -X POST http://your-api-url/api/RouteNode/navigateToLevel \
  -H "Content-Type: application/json" \
  -d '{
    "currentNodeId": 1,
    "targetLevel": 2
  }'
```

**Expected:** No database error. You might get:
- ✅ A valid path response
- ✅ "Node not found" error (means DB is fixed, just no data)
- ✅ "No path found" error (means DB is fixed, just no route)

**NOT Expected:**
- ❌ "column r.connected_levels does not exist"

---

## ⚡ Quick Decision Matrix

Choose based on your situation:

| Situation | Solution | File to Use |
|-----------|----------|-------------|
| I have Supabase access | **SQL Editor** ⭐ | `diagnose_and_fix_migration.sql` |
| I have terminal + psql | **Interactive Script** | `./quick_fix_script.sh` |
| I want to understand it | **Read docs first** | `SOLUTION_SUMMARY.md` |
| Quick fix, no frills | **Direct SQL** | `apply_connection_point_migration.sql` |
| Something went wrong | **Troubleshoot** | `TROUBLESHOOTING_MIGRATION_ERROR.md` |

---

## 🛡️ Safety Information

**All provided SQL scripts are SAFE:**

✅ Use `IF NOT EXISTS` - Won't break existing columns  
✅ Use `BEGIN`/`COMMIT` - Atomic transactions  
✅ Include verification queries  
✅ Won't delete any data  
✅ Can be run multiple times safely  

**Risk Level:** 🟢 LOW

The scripts only **ADD** columns, they never remove or modify existing data.

---

## 📞 Support

If you encounter issues:

1. Run `diagnose_and_fix_migration.sql` and share the output
2. Share your application startup logs
3. Confirm which database you're connecting to
4. Check if the migration is in `__EFMigrationsHistory` table

---

## 🎯 Next Steps

1. **Read** `README_COLUMN_FIX.md` (quick overview)
2. **Choose** your preferred fix method (Supabase SQL Editor recommended)
3. **Apply** the fix (5 minutes)
4. **Verify** the endpoint works
5. **Done!** ✅

---

## ⏰ Time Estimates

- **Reading this file:** 3 minutes
- **Applying fix via Supabase SQL Editor:** 5 minutes
- **Verifying fix:** 2 minutes
- **Total:** ~10 minutes

---

## 📌 Summary

**Problem:** Missing database columns causing API errors  
**Solution:** Run `diagnose_and_fix_migration.sql`  
**Method:** Supabase SQL Editor (easiest)  
**Time:** 5 minutes  
**Risk:** Low (safe to run)  
**Status:** Ready to apply  

---

**🚀 Ready to fix? Start with: `README_COLUMN_FIX.md`**

---

*This fix was automatically generated based on the error analysis and existing migration file: `20251201000000_AddConnectionPointFields.cs`*
