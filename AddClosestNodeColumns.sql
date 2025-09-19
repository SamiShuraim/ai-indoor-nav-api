-- Migration script to add closest_node_id and closest_node_distance columns to POI table
-- This should be run manually on the database

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

-- Update the migrations history table to mark this migration as applied
-- Note: Replace the migration ID with the actual timestamp when you create a proper migration
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
-- VALUES ('20250915213224_AddClosestNodeToPoi', '8.0.0');