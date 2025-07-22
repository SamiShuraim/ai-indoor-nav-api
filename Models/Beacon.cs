using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ai_indoor_nav_api.Models
{
    [Table("beacon_types")]
    public class BeaconType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public int? TransmissionPower { get; set; } // Signal strength

        public int? BatteryLife { get; set; } // Expected battery life in days

        [Column(TypeName = "decimal(6,2)")]
        public decimal? RangeMeters { get; set; } // Effective range in meters

        [JsonIgnore]
        public ICollection<Beacon> Beacons { get; set; } = new List<Beacon>();
    }

    public class Beacon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int FloorId { get; set; }

        public int? BeaconTypeId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; } = "";

        [StringLength(36)]
        public string? Uuid { get; set; } // Beacon UUID for physical identification

        public int? MajorId { get; set; } // iBeacon major ID

        public int? MinorId { get; set; } // iBeacon minor ID

        [Column(TypeName = "decimal(12,9)")]
        public decimal X { get; set; }

        [Column(TypeName = "decimal(12,9)")]
        public decimal Y { get; set; }

        [Column(TypeName = "decimal(8,4)")]
        public decimal Z { get; set; } = 0; // Height/elevation

        public bool IsActive { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public int BatteryLevel { get; set; } = 100; // Percentage

        public DateTime? LastSeen { get; set; }

        public DateOnly? InstallationDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("FloorId")]
        [JsonIgnore]
        public Floor? Floor { get; set; }

        [ForeignKey("BeaconTypeId")]
        public BeaconType? BeaconType { get; set; }
    }
} 