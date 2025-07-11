using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_indoor_nav_api.Models
{
    public class Poi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [Required]
        public int FloorId { get; set; }

        [ForeignKey("FloorId")]
        public Floor? Floor { get; set; }
    }
}