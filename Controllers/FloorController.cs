using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FloorController(MyDbContext context) : ControllerBase
    {
        // GET: api/Floor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Floor>>> GetFloors()
        {
            return await context.Floors
                .Include(f => f.Building)
                .OrderBy(f => f.BuildingId)
                .ThenBy(f => f.FloorNumber)
                .ToListAsync();
        }

        // GET: api/Floor/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Floor>> GetFloor(int id)
        {
            var floor = await context.Floors
                .Include(f => f.Building)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (floor == null)
            {
                return NotFound();
            }

            return floor;
        }

        // PUT: api/Floor/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFloor(int id, Floor floor)
        {
            if (id != floor.Id)
            {
                return BadRequest();
            }

            // Update the UpdatedAt timestamp
            floor.UpdatedAt = DateTime.UtcNow;

            context.Entry(floor).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FloorExists(id))
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

        // POST: api/Floor
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Floor>> PostFloor(Floor floor)
        {
            // Set timestamps
            floor.CreatedAt = DateTime.UtcNow;
            floor.UpdatedAt = DateTime.UtcNow;

            context.Floors.Add(floor);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetFloor", new { id = floor.Id }, floor);
        }

        // DELETE: api/Floor/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFloor(int id)
        {
            var floor = await context.Floors.FindAsync(id);
            if (floor == null)
            {
                return NotFound();
            }

            context.Floors.Remove(floor);
            await context.SaveChangesAsync();

            return NoContent();
        }
        
        // GET: api/Floor/building/5
        [HttpGet("building/{buildingId}")]
        public async Task<ActionResult<IEnumerable<Floor>>> GetFloorsByBuildingId(int buildingId)
        {
            return await context.Floors
                .Where(f => f.BuildingId == buildingId)
                .Include(f => f.Building)
                .OrderBy(f => f.FloorNumber)
                .ToListAsync();
        }

        private bool FloorExists(int id)
        {
            return context.Floors.Any(e => e.Id == id);
        }
    }
}