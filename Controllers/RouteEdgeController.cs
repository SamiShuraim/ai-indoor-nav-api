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
        public async Task<IActionResult> PutRouteEdge(int id, RouteEdge routeEdge)
        {
            if (id != routeEdge.Id)
            {
                return BadRequest();
            }

            // Update the UpdatedAt timestamp
            routeEdge.UpdatedAt = DateTime.UtcNow;

            context.Entry(routeEdge).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RouteEdgeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

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