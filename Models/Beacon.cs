using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    // The BeaconType model remains unchanged and is included for context
    [Table("beacon_types")]
    public class BeaconType
    {
        [Key]
        [Column("id")]
        public int Id { get; init; }

        [Required]
        [StringLength(100)]
        [Column("name")]
        public string Name { get; set; } = "";

        [Column("description")]
        public string? Description { get; set; }

        [Column("transmission_power")]
        public int? TransmissionPower { get; set; }

        [Column("battery_life")]
        public int? BatteryLife { get; set; }

        [Column("range_meters", TypeName = "numeric(6,2)")]
        public decimal? RangeMeters { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Beacon> Beacons { get; set; } = new List<Beacon>();
    }

    [Table("beacons")]
    public class Beacon
    {
        [Key] [Column("id")] public int Id { get; init; }

        [Required] [Column("floor_id")] public int FloorId { get; set; }

        [Column("beacon_type_id")] public int? BeaconTypeId { get; set; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = "";

        [StringLength(36)] [Column("uuid")] public string? Uuid { get; set; }

        [Column("major_id")] public int? MajorId { get; set; }

        [Column("minor_id")] public int? MinorId { get; set; }

        // EF Core will use the "geometry" column name and map it to a PostGIS 'Point'
        [Column("geometry", TypeName = "geometry (Point)")]
        public Point? Geometry { get; set; }

        [Column("is_active")] public bool IsActive { get; set; } = true;

        [Column("is_visible")] public bool IsVisible { get; set; } = true;

        [Column("battery_level")] public int BatteryLevel { get; set; } = 100;

        [Column("last_seen")] public DateTime? LastSeen { get; set; }

        [Column("installation_date")] public DateTime? InstallationDate { get; set; }

        [Column("created_at")] public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties for EF Core
        [ForeignKey("FloorId")] [Newtonsoft.Json.JsonIgnore] [System.Text.Json.Serialization.JsonIgnore] public Floor? Floor { get; set; }

        [ForeignKey("BeaconTypeId")] public BeaconType? BeaconType { get; set; }
    }
}