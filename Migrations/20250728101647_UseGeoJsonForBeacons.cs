using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class UseGeoJsonForBeacons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_floor_location",
                table: "beacons");

            migrationBuilder.DropColumn(
                name: "x",
                table: "beacons");

            migrationBuilder.DropColumn(
                name: "y",
                table: "beacons");

            migrationBuilder.DropColumn(
                name: "z",
                table: "beacons");

            migrationBuilder.AddColumn<Point>(
                name: "geometry",
                table: "beacons",
                type: "geometry (Point)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_beacons_floor_id",
                table: "beacons",
                column: "floor_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_beacons_floor_id",
                table: "beacons");

            migrationBuilder.DropColumn(
                name: "geometry",
                table: "beacons");

            migrationBuilder.AddColumn<decimal>(
                name: "x",
                table: "beacons",
                type: "numeric(12,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "y",
                table: "beacons",
                type: "numeric(12,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "z",
                table: "beacons",
                type: "numeric(8,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "idx_floor_location",
                table: "beacons",
                columns: new[] { "floor_id", "x", "y" });
        }
    }
}
