using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    [Table("route_edges")]
    public class RouteEdge
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("floor_id")]
        public int FloorId { get; set; }

        [Required]
        [Column("from_node_id")]
        public int FromNodeId { get; set; }

        [Required]
        [Column("to_node_id")]
        public int ToNodeId { get; set; }

        [Required]
        [Column("weight", TypeName = "numeric(8,4)")]
        public decimal Weight { get; set; } = 1.0m;

        [Required]
        [Column("edge_type")]
        [StringLength(50)]
        public string EdgeType { get; set; } = "walkable";

        [Column("is_bidirectional")]
        public bool IsBidirectional { get; set; } = true;

        [Column("is_visible")]
        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FloorId")]
        [JsonIgnore]
        public Floor? Floor { get; set; }

        [ForeignKey("FromNodeId")]
        [JsonIgnore]
        public RouteNode? FromNode { get; set; }

        [ForeignKey("ToNodeId")]
        [JsonIgnore]
        public RouteNode? ToNode { get; set; }
    }
}
