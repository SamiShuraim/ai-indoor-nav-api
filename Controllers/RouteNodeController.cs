using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteNodeController(MyDbContext context) : ControllerBase
    {
        // GET: api/RouteNode
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteNode>>> GetRouteNodes()
        {
            return await context.RouteNodes.ToListAsync();
        }

        // GET: api/RouteNode/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteNode>> GetRouteNode(int id)
        {
            var routeNode = await context.RouteNodes.FindAsync(id);

            if (routeNode == null)
            {
                return NotFound();
            }

            return routeNode;
        }

        // GET: api/RouteNode/floor/5
        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<IEnumerable<RouteNode>>> GetRouteNodesByFloorId(int floorId)
        {
            return await context.RouteNodes
                .Where(rn => rn.FloorId == floorId)
                .ToListAsync();
        }

        // GET: api/RouteNode/type/{nodeType}
        [HttpGet("type/{nodeType}")]
        public async Task<ActionResult<IEnumerable<RouteNode>>> GetRouteNodesByType(string nodeType)
        {
            return await context.RouteNodes
                .Where(rn => rn.NodeType == nodeType)
                .ToListAsync();
        }

        // PUT: api/RouteNode/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRouteNode(int id, RouteNode routeNode)
        {
            if (id != routeNode.Id)
            {
                return BadRequest();
            }

            // Update the UpdatedAt timestamp
            routeNode.UpdatedAt = DateTime.UtcNow;

            context.Entry(routeNode).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RouteNodeExists(id))
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

        // POST: api/RouteNode
        [HttpPost]
        public async Task<ActionResult<RouteNode>> PostRouteNode(RouteNode routeNode)
        {
            // Set timestamps
            routeNode.CreatedAt = DateTime.UtcNow;
            routeNode.UpdatedAt = DateTime.UtcNow;

            context.RouteNodes.Add(routeNode);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetRouteNode", new { id = routeNode.Id }, routeNode);
        }

        // DELETE: api/RouteNode/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteNode(int id)
        {
            var routeNode = await context.RouteNodes.FindAsync(id);
            if (routeNode == null)
            {
                return NotFound();
            }

            context.RouteNodes.Remove(routeNode);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool RouteNodeExists(int id)
        {
            return context.RouteNodes.Any(e => e.Id == id);
        }
    }
} 