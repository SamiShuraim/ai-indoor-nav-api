using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class rest_converted_from_snake_case : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_route_nodes_floors_FloorId",
                table: "route_nodes");

            migrationBuilder.RenameColumn(
                name: "Geometry",
                table: "route_nodes",
                newName: "geometry");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "route_nodes",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsVisible",
                table: "route_nodes",
                newName: "is_visible");

            migrationBuilder.RenameColumn(
                name: "FloorId",
                table: "route_nodes",
                newName: "floor_id");

            migrationBuilder.RenameIndex(
                name: "IX_route_nodes_Geometry",
                table: "route_nodes",
                newName: "IX_route_nodes_geometry");

            migrationBuilder.RenameIndex(
                name: "IX_route_nodes_FloorId",
                table: "route_nodes",
                newName: "IX_route_nodes_floor_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "poi_categories",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "poi_categories",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "poi_categories",
                newName: "color");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "poi_categories",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_poi_categories_Name",
                table: "poi_categories",
                newName: "IX_poi_categories_name");

            migrationBuilder.AlterColumn<Point>(
                name: "geometry",
                table: "route_nodes",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_route_nodes_floors_floor_id",
                table: "route_nodes",
                column: "floor_id",
                principalTable: "floors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_route_nodes_floors_floor_id",
                table: "route_nodes");

            migrationBuilder.RenameColumn(
                name: "geometry",
                table: "route_nodes",
                newName: "Geometry");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "route_nodes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_visible",
                table: "route_nodes",
                newName: "IsVisible");

            migrationBuilder.RenameColumn(
                name: "floor_id",
                table: "route_nodes",
                newName: "FloorId");

            migrationBuilder.RenameIndex(
                name: "IX_route_nodes_geometry",
                table: "route_nodes",
                newName: "IX_route_nodes_Geometry");

            migrationBuilder.RenameIndex(
                name: "IX_route_nodes_floor_id",
                table: "route_nodes",
                newName: "IX_route_nodes_FloorId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "poi_categories",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "poi_categories",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "color",
                table: "poi_categories",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "poi_categories",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_poi_categories_name",
                table: "poi_categories",
                newName: "IX_poi_categories_Name");

            migrationBuilder.AlterColumn<Point>(
                name: "Geometry",
                table: "route_nodes",
                type: "geometry",
                nullable: true,
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.AddForeignKey(
                name: "FK_route_nodes_floors_FloorId",
                table: "route_nodes",
                column: "FloorId",
                principalTable: "floors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
