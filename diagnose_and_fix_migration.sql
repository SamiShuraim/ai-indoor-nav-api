-- =====================================================
-- DIAGNOSTIC AND FIX SCRIPT FOR MISSING COLUMNS
-- =====================================================

-- STEP 1: Check what migrations are recorded as applied
-- =====================================================
SELECT * FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId" DESC 
LIMIT 10;

-- STEP 2: Check if the columns actually exist
-- =====================================================
SELECT 
    table_name,
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns
WHERE table_name = 'route_nodes'
ORDER BY ordinal_position;

-- STEP 3: Check specifically for the missing columns
-- =====================================================
SELECT 
    CASE 
        WHEN EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'route_nodes' AND column_name = 'is_connection_point')
        THEN '✓ is_connection_point exists'
        ELSE '✗ is_connection_point MISSING'
    END as status_1,
    CASE 
        WHEN EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'route_nodes' AND column_name = 'connection_type')
        THEN '✓ connection_type exists'
        ELSE '✗ connection_type MISSING'
    END as status_2,
    CASE 
        WHEN EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'route_nodes' AND column_name = 'connected_levels')
        THEN '✓ connected_levels exists'
        ELSE '✗ connected_levels MISSING'
    END as status_3,
    CASE 
        WHEN EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'route_nodes' AND column_name = 'connection_priority')
        THEN '✓ connection_priority exists'
        ELSE '✗ connection_priority MISSING'
    END as status_4;

-- =====================================================
-- FIX OPTION 1: If migration is marked as applied but columns are missing
-- =====================================================
-- This can happen if the migration was interrupted or failed partially

-- OPTION 1A: Remove the migration from history and let it reapply on next startup
-- (Only do this if you confirm the columns are missing)
-- DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251201000000_AddConnectionPointFields';

-- OPTION 1B: Manually add the columns (SAFE - uses IF NOT EXISTS)
-- Run this if you want to fix it immediately without restarting the app

BEGIN;

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

-- If migration is not in history, add it
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251201000000_AddConnectionPointFields', '8.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" 
    WHERE "MigrationId" = '20251201000000_AddConnectionPointFields'
);

COMMIT;

-- =====================================================
-- STEP 4: Verify the fix
-- =====================================================
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns
WHERE table_name = 'route_nodes'
    AND column_name IN ('is_connection_point', 'connection_type', 'connected_levels', 'connection_priority')
ORDER BY column_name;

-- Check indexes
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'route_nodes'
    AND indexname LIKE '%connection%'
ORDER BY indexname;

-- =====================================================
-- SUCCESS MESSAGE
-- =====================================================
SELECT '✓ Migration fix completed! You should see 4 columns above.' as message;
