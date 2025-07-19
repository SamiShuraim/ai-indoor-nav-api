using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_indoor_nav_api.Models
{
    // RouteNode model that matches the SQL schema exactly
    [Table("route_nodes")]
    public class RouteNode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FloorId { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal X { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal Y { get; set; }

        [StringLength(50)]
        public string NodeType { get; set; } = "waypoint"; // waypoint, entrance, exit, junction

        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FloorId")]
        public Floor? Floor { get; set; }
    }
}