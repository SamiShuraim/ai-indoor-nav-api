using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ai_indoor_nav_api.Models
{
    public class Floor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";
        
        [Required]
        public int FloorNumber { get; set; }

        [Required]
        public int BuildingId { get; set; }

        [ForeignKey("BuildingId")]
        public Building? Building { get; set; }

        public ICollection<Node> Nodes { get; set; } = new List<Node>();
        public ICollection<Poi> Pois { get; set; } = new List<Poi>();

        [Index(nameof(BuildingId), nameof(FloorNumber), IsUnique = true)]
        public class FloorNumberBuildingIndex { }
    }
}