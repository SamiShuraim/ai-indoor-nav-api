using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Data
{
    public class MyDbContext : IdentityDbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // RouteEdge configuration
            builder.Entity<RouteEdge>()
                .HasOne(e => e.FromNode)
                .WithMany()
                .HasForeignKey(e => e.FromNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RouteEdge>()
                .HasOne(e => e.ToNode)
                .WithMany()
                .HasForeignKey(e => e.ToNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RouteEdge>()
                .HasIndex(e => new { e.FromNodeId, e.ToNodeId })
                .IsUnique();

            builder.Entity<RouteEdge>()
                .HasIndex(e => new { e.FloorId, e.FromNodeId })
                .HasDatabaseName("idx_floor_from_node");

            builder.Entity<RouteEdge>()
                .HasIndex(e => new { e.FloorId, e.ToNodeId })
                .HasDatabaseName("idx_floor_to_node");

            builder.Entity<RouteEdge>()
               .HasCheckConstraint("no_self_reference", "\"from_node_id\" != \"to_node_id\"");

            // RouteNode indices
            builder.Entity<RouteNode>()
                .HasIndex(e => new { e.FloorId, e.X, e.Y })
                .HasDatabaseName("idx_floor_coordinates");

            // POI configuration
            builder.Entity<PoiCategory>()
                .HasIndex(pc => pc.Name)
                .IsUnique();

            builder.Entity<Poi>()
                .HasIndex(p => new { p.FloorId, p.CategoryId })
                .HasDatabaseName("idx_floor_category");

            // PoiPoint unique constraint
            builder.Entity<PoiPoint>()
                .HasIndex(pp => new { pp.PoiId, pp.PointOrder })
                .IsUnique();

            builder.Entity<PoiPoint>()
                .HasIndex(pp => new { pp.PoiId, pp.PointOrder })
                .HasDatabaseName("idx_poi_order");

            // Beacon configuration
            builder.Entity<BeaconType>()
                .HasIndex(bt => bt.Name)
                .IsUnique();

            builder.Entity<Beacon>()
                .HasIndex(b => new { b.Uuid, b.MajorId, b.MinorId })
                .IsUnique();

            builder.Entity<Beacon>()
                .HasIndex(b => new { b.FloorId, b.X, b.Y })
                .HasDatabaseName("idx_floor_location");

            builder.Entity<Beacon>()
                .HasIndex(b => new { b.Uuid, b.MajorId, b.MinorId })
                .HasDatabaseName("idx_beacon_identifiers");

            // Wall configuration
            builder.Entity<WallPoint>()
                .HasIndex(wp => new { wp.WallId, wp.PointOrder })
                .IsUnique();

            builder.Entity<WallPoint>()
                .HasIndex(wp => new { wp.WallId, wp.PointOrder })
                .HasDatabaseName("idx_wall_order");

            // Floor unique constraint
            builder.Entity<Floor>()
                .HasIndex(f => new { f.BuildingId, f.FloorNumber })
                .IsUnique();
        }

        // Core entities
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Floor> Floors { get; set; }
        public DbSet<Poi> Pois { get; set; }

        // Navigation entities
        public DbSet<RouteNode> RouteNodes { get; set; }
        public DbSet<RouteEdge> RouteEdges { get; set; }

        // POI entities
        public DbSet<PoiCategory> PoiCategories { get; set; }
        public DbSet<PoiPoint> PoiPoints { get; set; }

        // Beacon entities
        public DbSet<BeaconType> BeaconTypes { get; set; }
        public DbSet<Beacon> Beacons { get; set; }

        // Wall entities
        public DbSet<Wall> Walls { get; set; }
        public DbSet<WallPoint> WallPoints { get; set; }
    }
}
