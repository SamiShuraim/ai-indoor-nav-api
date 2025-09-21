using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ai_indoor_nav_api.Migrations
{
    /// <inheritdoc />
    public partial class FixAutoIncrementSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "beacon_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    transmission_power = table.Column<int>(type: "integer", nullable: true),
                    battery_life = table.Column<int>(type: "integer", nullable: true),
                    range_meters = table.Column<decimal>(type: "numeric(6,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beacon_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "buildings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buildings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "poi_categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poi_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "floors",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    floor_number = table.Column<int>(type: "integer", nullable: false),
                    building_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_floors", x => x.id);
                    table.ForeignKey(
                        name: "FK_floors_buildings_building_id",
                        column: x => x.building_id,
                        principalTable: "buildings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "beacons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    floor_id = table.Column<int>(type: "integer", nullable: false),
                    beacon_type_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    uuid = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    major_id = table.Column<int>(type: "integer", nullable: true),
                    minor_id = table.Column<int>(type: "integer", nullable: true),
                    geometry = table.Column<Point>(type: "geometry (Point)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    battery_level = table.Column<int>(type: "integer", nullable: false),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    installation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beacons", x => x.id);
                    table.ForeignKey(
                        name: "FK_beacons_beacon_types_beacon_type_id",
                        column: x => x.beacon_type_id,
                        principalTable: "beacon_types",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_beacons_floors_floor_id",
                        column: x => x.floor_id,
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_nodes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    floor_id = table.Column<int>(type: "integer", nullable: false),
                    connected_node_ids = table.Column<List<int>>(type: "integer[]", nullable: false),
                    geometry = table.Column<Point>(type: "geometry", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_nodes", x => x.id);
                    table.ForeignKey(
                        name: "FK_route_nodes_floors_floor_id",
                        column: x => x.floor_id,
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "poi",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    floor_id = table.Column<int>(type: "integer", nullable: false),
                    category_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    poi_type = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    geometry = table.Column<Polygon>(type: "geometry", nullable: true),
                    closest_node_id = table.Column<int>(type: "integer", nullable: true),
                    closest_node_distance = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poi", x => x.id);
                    table.ForeignKey(
                        name: "FK_poi_floors_floor_id",
                        column: x => x.floor_id,
                        principalTable: "floors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poi_poi_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "poi_categories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_poi_route_nodes_closest_node_id",
                        column: x => x.closest_node_id,
                        principalTable: "route_nodes",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_beacon_types_name",
                table: "beacon_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_beacon_identifiers",
                table: "beacons",
                columns: new[] { "uuid", "major_id", "minor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_beacons_beacon_type_id",
                table: "beacons",
                column: "beacon_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_beacons_floor_id",
                table: "beacons",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "IX_floors_building_id_floor_number",
                table: "floors",
                columns: new[] { "building_id", "floor_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_floor_category",
                table: "poi",
                columns: new[] { "floor_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_poi_category_id",
                table: "poi",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_poi_closest_node_id",
                table: "poi",
                column: "closest_node_id");

            migrationBuilder.CreateIndex(
                name: "IX_poi_categories_name",
                table: "poi_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_nodes_floor_id",
                table: "route_nodes",
                column: "floor_id");

            migrationBuilder.CreateIndex(
                name: "IX_route_nodes_geometry",
                table: "route_nodes",
                column: "geometry")
                .Annotation("Npgsql:IndexMethod", "GIST");

            // Reset sequences to ensure auto-increment works correctly
            // This is important if there are existing records with manually assigned IDs
            migrationBuilder.Sql(@"
                -- Reset sequence for buildings table
                SELECT setval(pg_get_serial_sequence('buildings', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM buildings;
                
                -- Reset sequence for floors table  
                SELECT setval(pg_get_serial_sequence('floors', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM floors;
                
                -- Reset sequence for beacons table
                SELECT setval(pg_get_serial_sequence('beacons', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM beacons;
                
                -- Reset sequence for beacon_types table
                SELECT setval(pg_get_serial_sequence('beacon_types', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM beacon_types;
                
                -- Reset sequence for poi table
                SELECT setval(pg_get_serial_sequence('poi', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM poi;
                
                -- Reset sequence for poi_categories table
                SELECT setval(pg_get_serial_sequence('poi_categories', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM poi_categories;
                
                -- Reset sequence for route_nodes table
                SELECT setval(pg_get_serial_sequence('route_nodes', 'id'), COALESCE(MAX(id), 1), MAX(id) IS NOT NULL) FROM route_nodes;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "beacons");

            migrationBuilder.DropTable(
                name: "poi");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "beacon_types");

            migrationBuilder.DropTable(
                name: "poi_categories");

            migrationBuilder.DropTable(
                name: "route_nodes");

            migrationBuilder.DropTable(
                name: "floors");

            migrationBuilder.DropTable(
                name: "buildings");
        }
    }
}
