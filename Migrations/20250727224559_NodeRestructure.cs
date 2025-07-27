using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class NodeRestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "route_edges");

            migrationBuilder.DropIndex(
                name: "idx_floor_coordinates",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "NodeType",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "X",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "Y",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "poi_categories");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "route_nodes",
                type: "geometry",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "connected_node_ids",
                table: "route_nodes",
                type: "integer[]",
                nullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "PoiType",
                table: "poi",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_route_nodes_FloorId",
                table: "route_nodes",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_route_nodes_Location",
                table: "route_nodes",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_route_nodes_FloorId",
                table: "route_nodes");

            migrationBuilder.DropIndex(
                name: "IX_route_nodes_Location",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "route_nodes");

            migrationBuilder.DropColumn(
                name: "connected_node_ids",
                table: "route_nodes");

            migrationBuilder.AddColumn<string>(
                name: "NodeType",
                table: "route_nodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "X",
                table: "route_nodes",
                type: "numeric(12,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Y",
                table: "route_nodes",
                type: "numeric(12,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "poi_categories",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PoiType",
                table: "poi",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "route_edges",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    floor_id = table.Column<int>(type: "integer", nullable: false),
                    from_node_id = table.Column<int>(type: "integer", nullable: false),
                    to_node_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edge_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_bidirectional = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(8,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_edges", x => x.id);
                    table.CheckConstraint("no_self_reference", "\"from_node_id\" != \"to_node_id\"");
                    table.ForeignKey(
                        name: "FK_route_edges_floors_floor_id",
                        column: x => x.floor_id,
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_route_edges_route_nodes_from_node_id",
                        column: x => x.from_node_id,
                        principalTable: "route_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_edges_route_nodes_to_node_id",
                        column: x => x.to_node_id,
                        principalTable: "route_nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_floor_coordinates",
                table: "route_nodes",
                columns: new[] { "FloorId", "X", "Y" });

            migrationBuilder.CreateIndex(
                name: "idx_floor_from_node",
                table: "route_edges",
                columns: new[] { "floor_id", "from_node_id" });

            migrationBuilder.CreateIndex(
                name: "idx_floor_to_node",
                table: "route_edges",
                columns: new[] { "floor_id", "to_node_id" });

            migrationBuilder.CreateIndex(
                name: "IX_route_edges_from_node_id_to_node_id",
                table: "route_edges",
                columns: new[] { "from_node_id", "to_node_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_edges_to_node_id",
                table: "route_edges",
                column: "to_node_id");
        }
    }
}
