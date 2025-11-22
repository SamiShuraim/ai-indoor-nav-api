using System.ComponentModel.DataAnnotations;

namespace ai_indoor_nav_api.Models
{
    public class LevelNavigationRequest
    {
        /// <summary>
        /// Current node ID (user's trilaterated position as a node)
        /// </summary>
        [Required]
        public int CurrentNodeId { get; set; }

        /// <summary>
        /// Target level to navigate to (1, 2, 3, etc.)
        /// </summary>
        [Required]
        public int TargetLevel { get; set; }

        /// <summary>
        /// Optional: Floor ID if known, otherwise will be inferred from the current node
        /// </summary>
        public int? FloorId { get; set; }
    }
}
