using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;
using static System.DateTime;

namespace ai_indoor_nav_api.Models
{
    [Table("route_nodes")]
    public class RouteNode
    {
        [Key]
        [Column("id")]
        public int Id { get; init; }

        [Required]
        [Column("floor_id")]
        public int FloorId { get; set; }
        
        [Column("connected_node_ids", TypeName = "integer[]")]
        public List<int> ConnectedNodeIds { get; set; } = new();  // Your edges

        [Required]
        [Column("geometry")]
        public Point? Geometry { get; set; }  // Now GeoJSON-compatible

        [Column("is_visible")]
        public bool IsVisible { get; set; } = true;

        [Column("level")]
        public int? Level { get; set; }

        [Column("level")]
        public int? Level { get; set; } = null;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = UtcNow;

        [ForeignKey("FloorId")]
        [JsonIgnore]
        public Floor? Floor { get; set; }
    }
}
