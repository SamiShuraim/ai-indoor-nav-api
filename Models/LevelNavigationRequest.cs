using System.ComponentModel.DataAnnotations;

namespace ai_indoor_nav_api.Models
{
    public class LevelNavigationRequest
    {
        /// <summary>
        /// Current position of the user (latitude/longitude)
        /// </summary>
        [Required]
        public LocationPoint? CurrentPosition { get; set; }

        /// <summary>
        /// Target level to navigate to (1, 2, 3, etc.)
        /// </summary>
        [Required]
        public int TargetLevel { get; set; }

        /// <summary>
        /// Optional: Floor ID if known, otherwise will default to floor 1
        /// </summary>
        public int? FloorId { get; set; }
    }
}
