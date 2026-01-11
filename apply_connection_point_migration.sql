-- Migration: AddConnectionPointFields
-- This script adds the connection point fields to the route_nodes table

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

-- Create index on is_connection_point for faster queries
CREATE INDEX IF NOT EXISTS idx_route_nodes_is_connection_point
ON route_nodes(is_connection_point);

-- Create index on connection_type for faster queries
CREATE INDEX IF NOT EXISTS idx_route_nodes_connection_type
ON route_nodes(connection_type)
INCLUDE (connection_priority, connected_levels);

-- Verify the columns were added
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns
WHERE table_name = 'route_nodes'
    AND column_name IN ('is_connection_point', 'connection_type', 'connected_levels', 'connection_priority')
ORDER BY column_name;

-- Show indexes
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'route_nodes'
    AND indexname LIKE '%connection%';
