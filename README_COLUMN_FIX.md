# 🔧 Fix for: "column r.connected_levels does not exist" Error

## 🚨 Problem

Your API is throwing:
```
PostgresException: 42703: column r.connected_levels does not exist
```

## ⚡ Quick Fix (Choose One)

### Option 1: Using Supabase SQL Editor (Easiest - No Installation Required)

1. Go to **https://app.supabase.com/**
2. Select your project
3. Click **SQL Editor** (left sidebar)
4. Open the file **`diagnose_and_fix_migration.sql`** from this folder
5. Copy its contents and paste into SQL Editor
6. Click **Run**
7. ✅ Done! Check for ✓ indicators in the output

---

### Option 2: Using the Interactive Script (If you have shell access)

```bash
cd /workspace
./quick_fix_script.sh
# Choose option 1: "Diagnose and fix"
```

**Requirements:**
- `.env` file with database credentials
- `psql` command installed

---

### Option 3: Using psql Command

```bash
PGPASSWORD='your_password' psql \
  -h your-supabase-host \
  -p 6543 \
  -U your-username \
  -d postgres \
  -f diagnose_and_fix_migration.sql
```

---

## 📁 Files Available

| File | What It Does |
|------|-------------|
| **`diagnose_and_fix_migration.sql`** ⭐ | Complete fix - Recommended! |
| **`quick_fix_script.sh`** | Interactive bash script |
| **`SOLUTION_SUMMARY.md`** | Detailed overview of all solutions |
| **`TROUBLESHOOTING_MIGRATION_ERROR.md`** | Deep troubleshooting guide |
| **`apply_connection_point_migration.sql`** | Simple migration SQL |
| **`FIX_MISSING_COLUMNS.md`** | General fix documentation |

---

## ✅ Verification

After applying the fix, test your API:

```bash
curl -X POST http://your-api-url/api/RouteNode/navigateToLevel \
  -H "Content-Type: application/json" \
  -d '{"currentNodeId": 1, "targetLevel": 2}'
```

Expected: No database error (you might get "node not found" which is OK)

---

## 📖 More Information

- **Quick overview:** Read `SOLUTION_SUMMARY.md`
- **Detailed troubleshooting:** Read `TROUBLESHOOTING_MIGRATION_ERROR.md`
- **Just want the SQL:** Use `apply_connection_point_migration.sql`

---

## 🎯 Recommended Path

1. **Use Supabase SQL Editor** (Option 1 above) - Easiest and safest
2. Run **`diagnose_and_fix_migration.sql`**
3. Look for **✓** indicators in the output
4. Test your API endpoint
5. Done! 🎉

---

**Estimated time to fix:** 5 minutes

**Risk level:** 🟢 Safe (uses `IF NOT EXISTS`, won't break existing columns)
