namespace ai_indoor_nav_api.Models;

public class RouteNodeDto
{
    public int FloorId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsVisible { get; set; } = true;

    public List<int> ConnectedNodeIds { get; set; } = new();
}