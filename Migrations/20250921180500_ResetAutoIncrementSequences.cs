using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class ResetAutoIncrementSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reset sequences to ensure auto-increment works correctly
            // This is important if there are existing records with manually assigned IDs
            migrationBuilder.Sql(@"
                -- Reset sequence for buildings table
                SELECT setval(pg_get_serial_sequence('buildings', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM buildings;
                
                -- Reset sequence for floors table  
                SELECT setval(pg_get_serial_sequence('floors', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM floors;
                
                -- Reset sequence for beacons table
                SELECT setval(pg_get_serial_sequence('beacons', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM beacons;
                
                -- Reset sequence for beacon_types table
                SELECT setval(pg_get_serial_sequence('beacon_types', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM beacon_types;
                
                -- Reset sequence for poi table
                SELECT setval(pg_get_serial_sequence('poi', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM poi;
                
                -- Reset sequence for poi_categories table
                SELECT setval(pg_get_serial_sequence('poi_categories', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM poi_categories;
                
                -- Reset sequence for route_nodes table
                SELECT setval(pg_get_serial_sequence('route_nodes', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM route_nodes;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down action needed - sequences will remain at their current values
        }
    }
}