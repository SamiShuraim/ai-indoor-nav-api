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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Set default query timeout to 60 seconds
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Poi>()
                .Property(p => p.PoiType)
                .HasConversion<string>();
            
            builder.Entity<RouteNode>()
                .HasIndex(r => r.Geometry)
                .HasMethod("GIST");

            // POI configuration
            builder.Entity<PoiCategory>()
                .HasIndex(pc => pc.Name)
                .IsUnique();

            builder.Entity<Poi>()
                .HasIndex(p => new { p.FloorId, p.CategoryId })
                .HasDatabaseName("idx_floor_category");

            // Beacon configuration
            builder.Entity<BeaconType>()
                .HasIndex(bt => bt.Name)
                .IsUnique();

            builder.Entity<Beacon>()
                .HasIndex(b => new { b.Uuid, b.MajorId, b.MinorId })
                .IsUnique();

            builder.Entity<Beacon>()
                .HasIndex(b => new { b.Uuid, b.MajorId, b.MinorId })
                .HasDatabaseName("idx_beacon_identifiers");

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

        // POI entities
        public DbSet<PoiCategory> PoiCategories { get; set; }

        // Beacon entities
        public DbSet<BeaconType> BeaconTypes { get; set; }
        public DbSet<Beacon> Beacons { get; set; }

    }
}
