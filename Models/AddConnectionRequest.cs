using System.ComponentModel.DataAnnotations;

namespace ai_indoor_nav_api.Models
{
    public class AddConnectionRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "NodeId1 must be a positive integer")]
        public int NodeId1 { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "NodeId2 must be a positive integer")]
        public int NodeId2 { get; set; }
    }
}