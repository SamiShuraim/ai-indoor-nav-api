using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WallController(MyDbContext context) : ControllerBase
    {
        // GET: api/Wall
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Wall>>> GetWalls()
        {
            return await context.Walls
                .Include(w => w.Floor)
                .Include(w => w.WallPoints)
                .ToListAsync();
        }

        // GET: api/Wall/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Wall>> GetWall(int id)
        {
            var wall = await context.Walls
                .Include(w => w.Floor)
                .Include(w => w.WallPoints.OrderBy(wp => wp.PointOrder))
                .FirstOrDefaultAsync(w => w.Id == id);

            if (wall == null)
            {
                return NotFound();
            }

            return wall;
        }

        // GET: api/Wall/floor/5
        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<IEnumerable<Wall>>> GetWallsByFloorId(int floorId)
        {
            return await context.Walls
                .Where(w => w.FloorId == floorId)
                .Include(w => w.Floor)
                .Include(w => w.WallPoints.OrderBy(wp => wp.PointOrder))
                .ToListAsync();
        }

        // GET: api/Wall/type/{wallType}
        [HttpGet("type/{wallType}")]
        public async Task<ActionResult<IEnumerable<Wall>>> GetWallsByType(string wallType)
        {
            return await context.Walls
                .Where(w => w.WallType == wallType)
                .Include(w => w.Floor)
                .Include(w => w.WallPoints.OrderBy(wp => wp.PointOrder))
                .ToListAsync();
        }

        // PUT: api/Wall/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWall(int id, Wall wall)
        {
            if (id != wall.Id)
            {
                return BadRequest();
            }

            // Update the UpdatedAt timestamp
            wall.UpdatedAt = DateTime.UtcNow;

            context.Entry(wall).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WallExists(id))
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

        // POST: api/Wall
        [HttpPost]
        public async Task<ActionResult<Wall>> PostWall(Wall wall)
        {
            // Set timestamps
            wall.CreatedAt = DateTime.UtcNow;
            wall.UpdatedAt = DateTime.UtcNow;

            context.Walls.Add(wall);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetWall", new { id = wall.Id }, wall);
        }

        // DELETE: api/Wall/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWall(int id)
        {
            var wall = await context.Walls.FindAsync(id);
            if (wall == null)
            {
                return NotFound();
            }

            context.Walls.Remove(wall);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool WallExists(int id)
        {
            return context.Walls.Any(e => e.Id == id);
        }
    }
} 