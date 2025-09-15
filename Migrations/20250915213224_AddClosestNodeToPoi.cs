using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class AddClosestNodeToPoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Only add the new columns to the existing poi table
            migrationBuilder.AddColumn<int>(
                name: "closest_node_id",
                table: "poi",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "closest_node_distance",
                table: "poi",
                type: "double precision",
                nullable: true);

            // Add foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_poi_closest_node_id",
                table: "poi",
                column: "closest_node_id");

            migrationBuilder.AddForeignKey(
                name: "FK_poi_route_nodes_closest_node_id",
                table: "poi",
                column: "closest_node_id",
                principalTable: "route_nodes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_poi_route_nodes_closest_node_id",
                table: "poi");

            migrationBuilder.DropIndex(
                name: "IX_poi_closest_node_id",
                table: "poi");

            migrationBuilder.DropColumn(
                name: "closest_node_distance",
                table: "poi");

            migrationBuilder.DropColumn(
                name: "closest_node_id",
                table: "poi");
        }
    }
}