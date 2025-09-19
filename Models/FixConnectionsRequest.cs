using System.ComponentModel.DataAnnotations;

namespace ai_indoor_nav_api.Models
{
    public class FixConnectionsRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Floor ID must be a positive integer")]
        public int FloorId { get; set; }
    }
}