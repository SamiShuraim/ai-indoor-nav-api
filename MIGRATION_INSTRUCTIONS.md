# Database Migration Instructions

The error you encountered indicates that the Entity Framework migration system is trying to create tables that already exist in your database. This is a common issue when the migration history gets out of sync.

## Problem

The migration `AddClosestNodeToPoi` was generated as a full database creation script instead of just adding the new columns. This happens when Entity Framework can't detect the existing database state.

## Solutions

Choose one of the following approaches:

### Option 1: Manual SQL Script (Recommended)

Run the SQL script provided in `AddClosestNodeColumns.sql` directly on your database:

```sql
-- Add the new columns to the poi table
ALTER TABLE poi 
ADD COLUMN IF NOT EXISTS closest_node_id integer,
ADD COLUMN IF NOT EXISTS closest_node_distance double precision;

-- Add foreign key constraint
ALTER TABLE poi 
ADD CONSTRAINT FK_poi_route_nodes_closest_node_id 
FOREIGN KEY (closest_node_id) REFERENCES route_nodes (id);

-- Add index for better performance
CREATE INDEX IF NOT EXISTS IX_poi_closest_node_id ON poi (closest_node_id);
```

Then mark the migration as applied in your database:

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20250915213224_AddClosestNodeToPoi', '8.0.0');
```

### Option 2: Reset Migration History

If you have a development environment and can afford to reset:

1. Drop the `__EFMigrationsHistory` table
2. Run `dotnet ef database update` to recreate it
3. The system will detect existing tables and only apply new changes

### Option 3: Force Migration State

Mark the existing migrations as applied without running them:

```bash
# Set environment variable for database connection
export DEFAULT_CONNECTION="your_connection_string_here"

# Update to specific migration (replace with your previous migration name)
dotnet ef database update 20250805191859_NodeGeometryInsteadOfLocation --connection "$DEFAULT_CONNECTION"

# Then apply the new migration
dotnet ef database update --connection "$DEFAULT_CONNECTION"
```

## Verification

After applying the migration, verify the changes:

```sql
-- Check if columns were added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'poi' 
AND column_name IN ('closest_node_id', 'closest_node_distance');

-- Check if foreign key constraint exists
SELECT constraint_name, table_name, column_name 
FROM information_schema.key_column_usage 
WHERE constraint_name = 'FK_poi_route_nodes_closest_node_id';
```

## Next Steps

Once the database schema is updated:

1. The pathfinding API will be fully functional
2. Creating new route nodes will automatically update POI closest nodes
3. You can use the `/api/RouteNode/findPath` endpoint for pathfinding

## Troubleshooting

If you continue to have issues:

1. Check that the `poi` and `route_nodes` tables exist
2. Verify the database connection string is correct
3. Ensure PostgreSQL PostGIS extension is installed
4. Check that the application has proper database permissions

The pathfinding implementation is complete and ready to use once the database schema is updated with these new columns.