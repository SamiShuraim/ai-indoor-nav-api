using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteNodePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create index on floor_id for faster floor-based queries
            migrationBuilder.CreateIndex(
                name: "idx_route_nodes_floor_id",
                table: "route_nodes",
                column: "floor_id");

            // Create index on level for faster level-based queries
            migrationBuilder.CreateIndex(
                name: "idx_route_nodes_level",
                table: "route_nodes",
                column: "level");

            // Create composite index on floor_id and level for cross-level navigation
            migrationBuilder.CreateIndex(
                name: "idx_route_nodes_floor_level",
                table: "route_nodes",
                columns: new[] { "floor_id", "level" });

            // Create composite index on is_visible and floor_id for filtered queries
            migrationBuilder.CreateIndex(
                name: "idx_route_nodes_visible_floor",
                table: "route_nodes",
                columns: new[] { "is_visible", "floor_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "idx_route_nodes_visible_floor",
                table: "route_nodes");

            migrationBuilder.DropIndex(
                name: "idx_route_nodes_floor_level",
                table: "route_nodes");

            migrationBuilder.DropIndex(
                name: "idx_route_nodes_level",
                table: "route_nodes");

            migrationBuilder.DropIndex(
                name: "idx_route_nodes_floor_id",
                table: "route_nodes");
        }
    }
}
