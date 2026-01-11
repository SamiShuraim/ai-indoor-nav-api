# Fix: Missing `connected_levels` Column Error

## Problem

The application is throwing this error:
```
42703: column r.connected_levels does not exist
```

This happens because the Entity Framework migration `20251201000000_AddConnectionPointFields` hasn't been applied to the database yet.

## Root Cause

The migration file exists in the codebase, but it hasn't been executed against the PostgreSQL database. The following columns are missing from the `route_nodes` table:
- `is_connection_point`
- `connection_type`
- `connected_levels`
- `connection_priority`

## Solutions

### Option 1: Apply Migration via .NET CLI (Recommended)

If you have the .NET SDK installed locally:

```bash
# Navigate to project directory
cd /workspace

# Apply all pending migrations
dotnet ef database update
```

### Option 2: Run SQL Script Directly

If you can't use the .NET CLI, use the provided SQL script:

**Step 1:** Get your database connection details from your `.env` file or Supabase dashboard.

**Step 2:** Connect to your database using one of these methods:

#### Using psql (PostgreSQL CLI):
```bash
# For Transaction Mode (Port 6543)
PGPASSWORD='your_password' psql \
  -h aws-1-ap-southeast-1.pooler.supabase.com \
  -p 6543 \
  -U postgres.xhvapujhplecxkqvepww \
  -d postgres \
  -f apply_connection_point_migration.sql
```

#### Using Supabase SQL Editor:
1. Go to https://app.supabase.com/
2. Select your project
3. Go to SQL Editor
4. Open the file `apply_connection_point_migration.sql`
5. Copy its contents and run in the SQL Editor

#### Using any PostgreSQL client (DBeaver, pgAdmin, etc.):
1. Connect using your connection string
2. Open and execute `apply_connection_point_migration.sql`

### Option 3: Manual SQL Execution

If you prefer to run the SQL manually, here's the complete script:

```sql
-- Add is_connection_point column
ALTER TABLE route_nodes
ADD COLUMN IF NOT EXISTS is_connection_point boolean NOT NULL DEFAULT false;

-- Add connection_type column
ALTER TABLE route_nodes
ADD COLUMN IF NOT EXISTS connection_type character varying(50) NULL;

-- Add connected_levels column
ALTER TABLE route_nodes
ADD COLUMN IF NOT EXISTS connected_levels integer[] NOT NULL DEFAULT '{}';

-- Add connection_priority column
ALTER TABLE route_nodes
ADD COLUMN IF NOT EXISTS connection_priority integer NULL;

-- Create index on is_connection_point
CREATE INDEX IF NOT EXISTS idx_route_nodes_is_connection_point
ON route_nodes(is_connection_point);

-- Create index on connection_type
CREATE INDEX IF NOT EXISTS idx_route_nodes_connection_type
ON route_nodes(connection_type)
INCLUDE (connection_priority, connected_levels);
```

### Option 4: Deploy with Migration Auto-Apply

If you're using Render or another deployment platform, you can configure the application to automatically apply migrations on startup.

**Add to your Dockerfile (before ENTRYPOINT):**
```dockerfile
# Install .NET SDK tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"
```

**Or update Program.cs to apply migrations on startup:**
```csharp
// In Program.cs, after building the app
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    db.Database.Migrate();  // Auto-apply pending migrations
}
```

## Verification

After applying the migration, verify the columns exist:

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

Expected output:
```
column_name          | data_type              | is_nullable | column_default
---------------------+------------------------+-------------+----------------
connected_levels     | ARRAY                  | NO          | '{}'::integer[]
connection_priority  | integer                | YES         | NULL
connection_type      | character varying      | YES         | NULL
is_connection_point  | boolean                | NO          | false
```

## Testing

After applying the migration, test the `/api/RouteNode/navigateToLevel` endpoint:

```bash
curl -X POST http://your-api-url/api/RouteNode/navigateToLevel \
  -H "Content-Type: application/json" \
  -d '{
    "currentNodeId": 1,
    "targetLevel": 2
  }'
```

The error should be resolved, and the endpoint should return a valid response or a meaningful error (like "No path found").

## Prevention

To avoid this in the future:

1. **Always apply migrations** when deploying new code that includes them
2. **Use automatic migration** on startup (Option 4 above)
3. **Test migrations** in a staging environment before production
4. **Version control** your database schema changes

## Files Created

- `apply_connection_point_migration.sql` - SQL script to manually apply the migration
- `FIX_MISSING_COLUMNS.md` - This documentation file
