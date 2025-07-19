using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ai_indoor_nav_api.Models
{
    [Index(nameof(BuildingId), nameof(FloorNumber), IsUnique = true)]
    public class Floor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";
        
        [Required]
        public int FloorNumber { get; set; }

        [Required]
        public int BuildingId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BuildingId")]
        public Building? Building { get; set; }

        public ICollection<RouteNode> RouteNodes { get; set; } = new List<RouteNode>();
        public ICollection<Poi> Pois { get; set; } = new List<Poi>();
        public ICollection<Beacon> Beacons { get; set; } = new List<Beacon>();
        public ICollection<Wall> Walls { get; set; } = new List<Wall>();
    }
}