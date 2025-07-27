using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class UseGeoJsonForPoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "poi_points");

            migrationBuilder.DropTable(
                name: "wall_points");

            migrationBuilder.DropTable(
                name: "walls");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Geometry>(
                name: "Geometry",
                table: "poi",
                type: "geometry",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Geometry",
                table: "poi");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "poi_points",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PoiId = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PointOrder = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<decimal>(type: "numeric(12,9)", nullable: false),
                    Y = table.Column<decimal>(type: "numeric(12,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poi_points", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poi_points_poi_PoiId",
                        column: x => x.PoiId,
                        principalTable: "poi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "walls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FloorId = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Height = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WallType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_walls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_walls_floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wall_points",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WallId = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PointOrder = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<decimal>(type: "numeric(12,9)", nullable: false),
                    Y = table.Column<decimal>(type: "numeric(12,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wall_points", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wall_points_walls_WallId",
                        column: x => x.WallId,
                        principalTable: "walls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_poi_order",
                table: "poi_points",
                columns: new[] { "PoiId", "PointOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_wall_order",
                table: "wall_points",
                columns: new[] { "WallId", "PointOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_walls_FloorId",
                table: "walls",
                column: "FloorId");
        }
    }
}
