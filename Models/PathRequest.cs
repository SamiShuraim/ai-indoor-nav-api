using System.ComponentModel.DataAnnotations;

namespace ai_indoor_nav_api.Models
{
    public class PathRequest
    {
        [Required]
        public LocationPoint? UserLocation { get; set; }

        [Required]
        public int DestinationPoiId { get; set; }
    }

    public class LocationPoint
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }
    }
}