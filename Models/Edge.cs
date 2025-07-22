using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    // RouteEdge model that matches the SQL schema exactly
    [Table("route_edges")]
    public class RouteEdge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FloorId { get; set; }

        [Required]
        public int FromNodeId { get; set; }

        [Required]
        public int ToNodeId { get; set; }

        [Column(TypeName = "decimal(8,4)")]
        public decimal Weight { get; set; } = 1.0m;

        [StringLength(50)]
        public string EdgeType { get; set; } = "walkable"; // walkable, stairs, elevator, etc.

        public bool IsBidirectional { get; set; } = true;

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