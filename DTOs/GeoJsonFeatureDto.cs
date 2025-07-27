namespace ai_indoor_nav_api.Models
{
    public class GeoJsonFeatureDto
    {
        public GeometryDto Geometry { get; set; } = null!;
        public PoiPropertiesDto Properties { get; set; } = null!;
    }

    public class GeometryDto
    {
        public string Type { get; set; } = null!;
        public object Coordinates { get; set; } = null!;
    }

    public class PoiPropertiesDto
    {
        public string Name { get; set; } = null!;
        public int FloorId { get; set; }
        public int? CategoryId { get; set; }
        public string? Description { get; set; }
        public string PoiType { get; set; } = "room";
        public string Color { get; set; } = "#3B82F6";
        public bool IsVisible { get; set; } = true;
    }
}