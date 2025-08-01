using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ai_indoor_nav_api.Enums;
using NetTopologySuite.Geometries;
using static System.DateTime;

namespace ai_indoor_nav_api.Models
{
    [Table("poi_categories")]
    public class PoiCategory
    {
        [Key]
        public int Id { get; init; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [StringLength(7)]
        public string Color { get; set; } = "#3B82F6"; // Hex color code

        public string? Description { get; set; }

        [JsonIgnore]
        public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    }
    
    [Table("poi")]
    public class Poi
    {
        [Key]
        public int Id { get; init; }

        [Required]
        public int FloorId { get; init; }

        public int? CategoryId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        [Required]
        [EnumDataType(typeof(PoiType))]
        public PoiType PoiType { get; set; } = PoiType.Room;

        [StringLength(7)]
        public string Color { get; set; } = "#3B82F6";

        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; init; } = UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = UtcNow;

        // Geometry field: Point, Polygon, etc.
        public Polygon? Geometry { get; set; }

        [ForeignKey("FloorId")]
        [JsonIgnore]
        public Floor? Floor { get; init; }

        [ForeignKey("CategoryId")]
        public PoiCategory? Category { get; set; }
    }
}
