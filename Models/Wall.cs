using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    [Table("walls")]
    public class Wall
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FloorId { get; set; }

        [StringLength(255)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string WallType { get; set; } = "interior"; // interior, exterior, glass, door

        [Column(TypeName = "decimal(6,2)")]
        public decimal Height { get; set; } = 3.0m; // Wall height in meters

        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FloorId")]
        [JsonIgnore]
        public Floor? Floor { get; set; }

        public ICollection<WallPoint> WallPoints { get; set; } = new List<WallPoint>();
    }

    [Table("wall_points")]
    public class WallPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int WallId { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal X { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal Y { get; set; }

        public int PointOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("WallId")]
        [JsonIgnore]
        public Wall? Wall { get; set; }
    }
}
