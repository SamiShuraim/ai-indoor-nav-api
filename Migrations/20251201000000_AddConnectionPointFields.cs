using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class AddConnectionPointFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add is_connection_point column
            migrationBuilder.AddColumn<bool>(
                name: "is_connection_point",
                table: "route_nodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Add connection_type column
            migrationBuilder.AddColumn<string>(
                name: "connection_type",
                table: "route_nodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Add connected_levels column
            migrationBuilder.AddColumn<int[]>(
                name: "connected_levels",
                table: "route_nodes",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[] { });

            // Add connection_priority column
            migrationBuilder.AddColumn<int>(
                name: "connection_priority",
                table: "route_nodes",
                type: "integer",
                nullable: true);

            // Create index on is_connection_point for faster queries
            migrationBuilder.CreateIndex(
                name: "idx_route_nodes_is_connection_point",
                table: "route_nodes",
                column: "is_connection_point");

            // Create index on connection_type for faster queries
            migrationBuilder.CreateIndex(
                name: "idx_route_nodes_connection_type",
                table: "route_nodes",
                column: "connection_type")
                .Annotation("Npgsql:IndexInclude", new[] { "connection_priority", "connected_levels" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "idx_route_nodes_connection_type",
                table: "route_nodes");

            migrationBuilder.DropIndex(
                name: "idx_route_nodes_is_connection_point",
                table: "route_nodes");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "connection_priority",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "connected_levels",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "connection_type",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "is_connection_point",
                table: "route_nodes");
        }
    }
}
