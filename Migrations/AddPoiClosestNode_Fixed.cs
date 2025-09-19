using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class AddPoiClosestNode : Migration
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

            // Create index for the new foreign key column
            migrationBuilder.CreateIndex(
                name: "IX_poi_closest_node_id",
                table: "poi",
                column: "closest_node_id");

            // Add foreign key constraint
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
            // Remove the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_poi_route_nodes_closest_node_id",
                table: "poi");

            // Remove the index
            migrationBuilder.DropIndex(
                name: "IX_poi_closest_node_id",
                table: "poi");

            // Remove the columns
            migrationBuilder.DropColumn(
                name: "closest_node_distance",
                table: "poi");

            migrationBuilder.DropColumn(
                name: "closest_node_id",
                table: "poi");
        }
    }
}