using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace ai_indoor_nav_api.Models
{
    [Table("floors")]
    [Index(nameof(BuildingId), nameof(FloorNumber), IsUnique = true)]
    public class Floor
    {
        [Key]
        [Column("id")]
        public int Id { get; init; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = "";

        [Required]
        [Column("floor_number")]
        public int FloorNumber { get; set; }

        [Required]
        [Column("building_id")]
        public int BuildingId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BuildingId")]
        [JsonIgnore]
        public Building? Building { get; set; }

        [JsonIgnore]
        public ICollection<RouteNode> RouteNodes { get; set; } = new List<RouteNode>();

        [JsonIgnore]
        public ICollection<Poi> Pois { get; set; } = new List<Poi>();

        [JsonIgnore]
        public ICollection<Beacon> Beacons { get; set; } = new List<Beacon>();
    }
}
