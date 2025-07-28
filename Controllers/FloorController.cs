using System.Text.Json;
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
        // GET: api/Floor?building=3
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Floor>>> GetFloors([FromQuery] int? building)
        {
            var query = context.Floors
                .Include(f => f.Building)
                .AsQueryable();

            if (building.HasValue)
            {
                query = query.Where(f => f.BuildingId == building.Value);
            }

            var floors = await query
                .OrderBy(f => f.BuildingId)
                .ThenBy(f => f.FloorNumber)
                .ToListAsync();

            return Ok(floors);
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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFloor(int id, JsonElement jsonFloor)
        {
            var existingFloor = await context.Floors.FindAsync(id);
            if (existingFloor == null)
                return NotFound();

            foreach (var prop in jsonFloor.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "name":
                        existingFloor.Name = prop.Value.GetString() ?? "";
                        break;

                    case "floornumber":
                        if (prop.Value.TryGetInt32(out var floorNumber))
                            existingFloor.FloorNumber = floorNumber;
                        break;

                    case "buildingid":
                        if (prop.Value.TryGetInt32(out var buildingId))
                            existingFloor.BuildingId = buildingId;
                        break;
                }
            }

            existingFloor.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
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

        private bool FloorExists(int id)
        {
            return context.Floors.Any(e => e.Id == id);
        }
    }
}