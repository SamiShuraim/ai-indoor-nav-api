using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteEdgeController(MyDbContext context) : ControllerBase
    {
        // GET: api/RouteEdge
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteEdge>>> GetRouteEdges()
        {
            return await context.RouteEdges.ToListAsync();
        }

        // GET: api/RouteEdge/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteEdge>> GetRouteEdge(int id)
        {
            var routeEdge = await context.RouteEdges.FindAsync(id);

            if (routeEdge == null)
            {
                return NotFound();
            }

            return routeEdge;
        }

        // GET: api/RouteEdge/floor/5
        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<IEnumerable<RouteEdge>>> GetRouteEdgesByFloorId(int floorId)
        {
            return await context.RouteEdges
                .Where(re => re.FloorId == floorId)
                .ToListAsync();
        }

        // GET: api/RouteEdge/type/{edgeType}
        [HttpGet("type/{edgeType}")]
        public async Task<ActionResult<IEnumerable<RouteEdge>>> GetRouteEdgesByType(string edgeType)
        {
            return await context.RouteEdges
                .Where(re => re.EdgeType == edgeType)
                .ToListAsync();
        }

        // GET: api/RouteEdge/node/{nodeId}
        [HttpGet("node/{nodeId}")]
        public async Task<ActionResult<IEnumerable<RouteEdge>>> GetRouteEdgesByNodeId(int nodeId)
        {
            return await context.RouteEdges
                .Where(re => re.FromNodeId == nodeId || re.ToNodeId == nodeId)
                .ToListAsync();
        }

        // PUT: api/RouteEdge/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRouteEdge(int id, JsonElement jsonRouteEdge)
        {
            var existingEdge = await context.RouteEdges.FindAsync(id);
            if (existingEdge == null)
                return NotFound();

            foreach (var prop in jsonRouteEdge.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "floorid":
                        if (prop.Value.TryGetInt32(out var floorId))
                            existingEdge.FloorId = floorId;
                        break;

                    case "fromnodeid":
                        if (prop.Value.TryGetInt32(out var fromNodeId))
                            existingEdge.FromNodeId = fromNodeId;
                        break;

                    case "tonodeid":
                        if (prop.Value.TryGetInt32(out var toNodeId))
                            existingEdge.ToNodeId = toNodeId;
                        break;

                    case "weight":
                        if (prop.Value.TryGetDecimal(out var weight))
                            existingEdge.Weight = weight;
                        break;

                    case "edgetype":
                        existingEdge.EdgeType = prop.Value.GetString() ?? "walkable";
                        break;

                    case "isbidirectional":
                        if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                            existingEdge.IsBidirectional = prop.Value.GetBoolean();
                        break;

                    case "isvisible":
                        if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                            existingEdge.IsVisible = prop.Value.GetBoolean();
                        break;
                }
            }

            existingEdge.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/RouteEdge
        [HttpPost]
        public async Task<ActionResult<RouteEdge>> PostRouteEdge(RouteEdge routeEdge)
        {
            // Set timestamps
            routeEdge.CreatedAt = DateTime.UtcNow;
            routeEdge.UpdatedAt = DateTime.UtcNow;

            context.RouteEdges.Add(routeEdge);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetRouteEdge", new { id = routeEdge.Id }, routeEdge);
        }

        // DELETE: api/RouteEdge/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteEdge(int id)
        {
            var routeEdge = await context.RouteEdges.FindAsync(id);
            if (routeEdge == null)
            {
                return NotFound();
            }

            context.RouteEdges.Remove(routeEdge);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool RouteEdgeExists(int id)
        {
            return context.RouteEdges.Any(e => e.Id == id);
        }
    }
} 