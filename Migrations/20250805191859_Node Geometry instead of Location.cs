using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class NodeGeometryinsteadofLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Location",
                table: "route_nodes",
                newName: "Geometry");

            migrationBuilder.RenameIndex(
                name: "IX_route_nodes_Location",
                table: "route_nodes",
                newName: "IX_route_nodes_Geometry");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Geometry",
                table: "route_nodes",
                newName: "Location");

            migrationBuilder.RenameIndex(
                name: "IX_route_nodes_Geometry",
                table: "route_nodes",
                newName: "IX_route_nodes_Location");
        }
    }
}
