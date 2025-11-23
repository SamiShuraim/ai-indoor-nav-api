using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using ai_indoor_nav_api.Enums;
using NetTopologySuite.Geometries;
using static System.DateTime;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    [Table("poi_categories")]
    public class PoiCategory
    {
        [Key]
        [Column("id")]
        public int Id { get; init; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        public string Name { get; set; } = "";

        [StringLength(7)]
        [Column("color")]
        public string Color { get; set; } = "#3B82F6"; // Hex color code

        [Column("description")]
        public string? Description { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    }
    
    [Table("poi")]
    public class Poi
    {
        [Key]
        [Column("id")]
        public int Id { get; init; }

        [Required]
        [Column("floor_id")]
        public int FloorId { get; init; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = "";

        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [EnumDataType(typeof(PoiType))]
        [Column("poi_type")]
        public PoiType PoiType { get; set; } = PoiType.Room;

        [StringLength(7)]
        [Column("color")]
        public string Color { get; set; } = "#3B82F6";

        [Column("is_visible")]
        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; init; } = UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = UtcNow;

        // Geometry field: Point, Polygon, etc.
        [Column("geometry")]
        public Polygon? Geometry { get; set; }

        // Navigation - closest route node for pathfinding
        [Column("closest_node_id")]
        public int? ClosestNodeId { get; set; }

        [Column("closest_node_distance")]
        public double? ClosestNodeDistance { get; set; }

        [ForeignKey("FloorId")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public Floor? Floor { get; init; }

        [ForeignKey("CategoryId")]
        public PoiCategory? Category { get; set; }

        [ForeignKey("ClosestNodeId")]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public RouteNode? ClosestNode { get; set; }
    }
}
