using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Data
{
    public class MyDbContext(DbContextOptions<MyDbContext> options) : IdentityDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Edge>()
                .HasOne(e => e.FromNode)
                .WithMany()
                .HasForeignKey(e => e.FromNodeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Edge>()
                .HasOne(e => e.ToNode)
                .WithMany()
                .HasForeignKey(e => e.ToNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
        
        public DbSet<Building> Buildings { get; set; } = null!;
        public DbSet<Floor> Floors { get; set; } = null!;
        public DbSet<Node> Nodes { get; set; } = null!;
        public DbSet<Edge> Edges { get; set; } = null!;
        public DbSet<Poi> Pois { get; set; } = null!;
    }
}