using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    [Table("poi_categories")]
    public class PoiCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(7)]
        public string Color { get; set; } = "#3B82F6"; // Hex color code

        [StringLength(50)]
        public string? Icon { get; set; } // Icon identifier

        public string? Description { get; set; }

        [JsonIgnore]
        public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    }

    public class Poi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FloorId { get; set; }

        public int? CategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        [StringLength(50)]
        public string PoiType { get; set; } = "room"; // room, area, zone, facility

        [StringLength(7)]
        public string Color { get; set; } = "#3B82F6";

        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FloorId")]
        [JsonIgnore]
        public Floor? Floor { get; set; }

        [ForeignKey("CategoryId")]
        public PoiCategory? Category { get; set; }

        public ICollection<PoiPoint> PoiPoints { get; set; } = new List<PoiPoint>();
    }

    [Table("poi_points")]
    public class PoiPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PoiId { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal X { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal Y { get; set; }

        public int PointOrder { get; set; } // Order of points in polygon

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PoiId")]
        [JsonIgnore]
        public Poi? Poi { get; set; }
    }
}