using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class POI_props_turned_snake_case : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_poi_floors_FloorId",
                table: "poi");

            migrationBuilder.DropForeignKey(
                name: "FK_poi_poi_categories_CategoryId",
                table: "poi");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "poi",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Geometry",
                table: "poi",
                newName: "geometry");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "poi",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "poi",
                newName: "color");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "poi",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PoiType",
                table: "poi",
                newName: "poi_type");

            migrationBuilder.RenameColumn(
                name: "IsVisible",
                table: "poi",
                newName: "is_visible");

            migrationBuilder.RenameColumn(
                name: "FloorId",
                table: "poi",
                newName: "floor_id");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "poi",
                newName: "category_id");

            migrationBuilder.RenameIndex(
                name: "IX_poi_CategoryId",
                table: "poi",
                newName: "IX_poi_category_id");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "poi",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "floor_id1",
                table: "poi",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_poi_floor_id1",
                table: "poi",
                column: "floor_id1");

            migrationBuilder.AddForeignKey(
                name: "FK_poi_floors_floor_id1",
                table: "poi",
                column: "floor_id1",
                principalTable: "floors",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_poi_poi_categories_category_id",
                table: "poi",
                column: "category_id",
                principalTable: "poi_categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_poi_floors_floor_id1",
                table: "poi");

            migrationBuilder.DropForeignKey(
                name: "FK_poi_poi_categories_category_id",
                table: "poi");

            migrationBuilder.DropIndex(
                name: "IX_poi_floor_id1",
                table: "poi");

            migrationBuilder.DropColumn(
                name: "floor_id1",
                table: "poi");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "poi",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "geometry",
                table: "poi",
                newName: "Geometry");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "poi",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "color",
                table: "poi",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "poi",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "poi_type",
                table: "poi",
                newName: "PoiType");

            migrationBuilder.RenameColumn(
                name: "is_visible",
                table: "poi",
                newName: "IsVisible");

            migrationBuilder.RenameColumn(
                name: "floor_id",
                table: "poi",
                newName: "FloorId");

            migrationBuilder.RenameColumn(
                name: "category_id",
                table: "poi",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_poi_category_id",
                table: "poi",
                newName: "IX_poi_CategoryId");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "poi",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_poi_floors_FloorId",
                table: "poi",
                column: "FloorId",
                principalTable: "floors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poi_poi_categories_CategoryId",
                table: "poi",
                column: "CategoryId",
                principalTable: "poi_categories",
                principalColumn: "Id");
        }
    }
}
