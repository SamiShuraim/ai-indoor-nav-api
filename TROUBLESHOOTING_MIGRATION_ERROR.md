# Troubleshooting: Migration Error - Column `connected_levels` Does Not Exist

## Error Details

```
PostgresException (0x80004005): 42703: column r.connected_levels does not exist
```

**Location:** `RouteNodeController.cs:450` in the `NavigateToLevel` endpoint

## Root Cause Analysis

The application expects columns that were added in migration `20251201000000_AddConnectionPointFields`, but the database doesn't have them. This indicates one of these scenarios:

### Scenario 1: Migration Failed Silently
The automatic migration code in `Program.cs` (lines 179-234) tried to apply the migration but failed, yet the migration was marked as applied in `__EFMigrationsHistory`.

### Scenario 2: Database Schema Out of Sync
The application is running against a database that doesn't have the latest schema changes, possibly due to:
- Using a different database than expected
- Migration was applied to a different database instance
- Database was restored from an old backup

### Scenario 3: Migration Entry Missing
The migration file exists in the codebase but was never executed against the production database.

## Diagnosis Steps

### Step 1: Check Application Logs

Look at the application startup logs for migration messages:

```
Checking database connection and applying migrations...
Applying N pending migration(s)...
Migrations applied successfully
```

or

```
Database is up to date, no migrations needed
```

If you see "no migrations needed" but the columns are missing, this confirms Scenario 1.

### Step 2: Run Diagnostic SQL

Connect to your database and run the diagnostic script:

```bash
# Connect to your Supabase database
psql -h aws-1-ap-southeast-1.pooler.supabase.com \
     -p 6543 \
     -U postgres.xhvapujhplecxkqvepww \
     -d postgres \
     -f diagnose_and_fix_migration.sql
```

Or use the Supabase SQL Editor:
1. Go to https://app.supabase.com/
2. Select your project
3. Go to SQL Editor
4. Open and run `diagnose_and_fix_migration.sql`

This will show:
- Which migrations are recorded as applied
- Which columns actually exist
- Clear status indicators (✓ or ✗) for each required column

## Solution Methods

### Method 1: Auto-Fix SQL Script (Recommended - Safest)

Run the complete diagnostic and fix script:

```bash
psql -h YOUR_HOST -p YOUR_PORT -U YOUR_USER -d YOUR_DB \
     -f diagnose_and_fix_migration.sql
```

This script:
- ✓ Uses `IF NOT EXISTS` to safely add missing columns
- ✓ Won't break if columns already exist
- ✓ Creates the necessary indexes
- ✓ Updates the migration history
- ✓ Provides verification output

### Method 2: Force Migration Reapply

If you're certain the columns are missing and the migration is marked as applied:

```sql
-- Remove the migration from history
DELETE FROM "__EFMigrationsHistory" 
WHERE "MigrationId" = '20251201000000_AddConnectionPointFields';

-- Restart your application
-- The automatic migration code will reapply it
```

⚠️ **Warning:** Only do this if you're absolutely sure the migration needs to be reapplied.

### Method 3: Manual Column Addition (Quick Fix)

If you need an immediate fix without restarting:

```sql
BEGIN;

ALTER TABLE route_nodes ADD COLUMN IF NOT EXISTS is_connection_point boolean NOT NULL DEFAULT false;
ALTER TABLE route_nodes ADD COLUMN IF NOT EXISTS connection_type character varying(50) NULL;
ALTER TABLE route_nodes ADD COLUMN IF NOT EXISTS connected_levels integer[] NOT NULL DEFAULT '{}';
ALTER TABLE route_nodes ADD COLUMN IF NOT EXISTS connection_priority integer NULL;

CREATE INDEX IF NOT EXISTS idx_route_nodes_is_connection_point ON route_nodes(is_connection_point);
CREATE INDEX IF NOT EXISTS idx_route_nodes_connection_type ON route_nodes(connection_type) 
  INCLUDE (connection_priority, connected_levels);

-- Mark migration as applied if not already
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251201000000_AddConnectionPointFields', '8.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20251201000000_AddConnectionPointFields'
);

COMMIT;
```

### Method 4: Redeploy with Clean Migration

If you're using Render or another deployment platform:

1. **Connect to your database** and verify current state
2. **Remove the problematic migration** from history (if needed)
3. **Redeploy** your application
4. **Monitor startup logs** to confirm migration applies successfully

## Verification

After applying any fix, verify the columns exist:

```sql
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns
WHERE table_name = 'route_nodes'
    AND column_name IN ('is_connection_point', 'connection_type', 'connected_levels', 'connection_priority')
ORDER BY column_name;
```

Expected result: **4 rows** showing all four columns.

Then test the endpoint:

```bash
curl -X POST https://your-api-url/api/RouteNode/navigateToLevel \
  -H "Content-Type: application/json" \
  -d '{
    "currentNodeId": 1,
    "targetLevel": 2
  }'
```

You should either get a valid response or a meaningful error (not a database error).

## Prevention for Future Deployments

### 1. Enable Migration Logs

The automatic migration code in `Program.cs` already has good logging. Ensure you're monitoring these logs during deployments:

```csharp
Console.WriteLine("Checking database connection and applying migrations...");
Console.WriteLine($"Applying {pendingCount} pending migration(s)...");
```

### 2. Add Health Check for Migrations

Consider adding a health check endpoint that verifies critical columns exist:

```csharp
[HttpGet("health/schema")]
public async Task<IActionResult> CheckSchema()
{
    var sql = @"
        SELECT column_name 
        FROM information_schema.columns 
        WHERE table_name = 'route_nodes' 
            AND column_name = 'connected_levels'";
    
    var result = await _context.Database
        .ExecuteSqlRawAsync(sql);
    
    return Ok(new { schemaValid = result > 0 });
}
```

### 3. Use Deployment Scripts

Create a pre-deployment script that checks for pending migrations:

```bash
#!/bin/bash
# check-migrations.sh

echo "Checking for pending migrations..."
dotnet ef migrations list

echo "Applying all pending migrations..."
dotnet ef database update

echo "Verifying migration status..."
dotnet ef migrations list
```

### 4. Database Backup Before Deployments

Always backup your database before applying schema changes:

```bash
# For Supabase/PostgreSQL
pg_dump -h YOUR_HOST -p YOUR_PORT -U YOUR_USER -d YOUR_DB > backup_$(date +%Y%m%d_%H%M%S).sql
```

## Understanding the Automatic Migration Code

Your `Program.cs` already has excellent automatic migration code (lines 179-234):

```csharp
// Apply migrations with timeout and error handling
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    
    try
    {
        Console.WriteLine("Checking database connection and applying migrations...");
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cts.Token);
        var pendingCount = pendingMigrations.Count();
        
        if (pendingCount > 0)
        {
            Console.WriteLine($"Applying {pendingCount} pending migration(s)...");
            await db.Database.MigrateAsync(cts.Token);
            Console.WriteLine("Migrations applied successfully");
        }
        else
        {
            Console.WriteLine("Database is up to date, no migrations needed");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR during migration: {ex.Message}");
        throw;
    }
}
```

This means:
- ✓ Migrations **should** apply automatically on app startup
- ✓ You'll see clear log messages about migration status
- ✓ The app will crash if migrations fail (preventing use of incorrect schema)

If the error occurred despite this code, it likely means:
1. The migration is marked as applied in `__EFMigrationsHistory`
2. But the actual schema changes didn't complete
3. Check your deployment logs for migration errors

## Quick Reference

| Problem | Solution | Risk Level |
|---------|----------|-----------|
| Columns missing, migration not in history | Run `apply_connection_point_migration.sql` | Low (safe) |
| Columns missing, migration IS in history | Run `diagnose_and_fix_migration.sql` | Low (safe) |
| Need immediate fix | Method 3: Manual column addition | Low (safe) |
| Want clean state | Method 2: Force reapply | Medium |
| Persistent issues | Method 4: Redeploy | Medium |

## Files Created

1. **`diagnose_and_fix_migration.sql`** - Complete diagnostic and fix script (RECOMMENDED)
2. **`apply_connection_point_migration.sql`** - Simple column addition script
3. **`FIX_MISSING_COLUMNS.md`** - General fix documentation
4. **`TROUBLESHOOTING_MIGRATION_ERROR.md`** - This detailed troubleshooting guide

## Support

If issues persist after trying these solutions:

1. Share your application startup logs
2. Share the output of `diagnose_and_fix_migration.sql`
3. Confirm which database you're connecting to
4. Check if you have multiple database instances

## Summary

**Immediate Action Required:**
1. Run `diagnose_and_fix_migration.sql` against your production database
2. Verify columns are added (should see ✓ indicators)
3. Test the `/api/RouteNode/navigateToLevel` endpoint
4. Monitor application logs for any migration warnings

This will resolve your issue immediately and safely!
