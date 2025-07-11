using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_indoor_nav_api.Models
{
    public class Node
    {
        [Key]
        public int Id { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string? Description { get; set; }

        [Required]
        public int FloorId { get; set; }

        [ForeignKey("FloorId")]
        public Floor? Floor { get; set; }
    }
}