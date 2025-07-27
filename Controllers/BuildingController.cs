using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BuildingController(MyDbContext context) : ControllerBase
    {
        // GET: api/Building
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Building>>> GetBuildings()
        {
            return await context.Buildings.ToListAsync();
        }

        // GET: api/Building/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Building>> GetBuilding(int id)
        {
            var building = await context.Buildings.FindAsync(id);

            if (building == null)
            {
                return NotFound();
            }

            return building;
        }

        // PUT: api/Building/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBuilding(int id, JsonElement jsonBuilding)
        {
            var existingBuilding = await context.Buildings.FindAsync(id);
            if (existingBuilding == null)
                return NotFound();

            foreach (var prop in jsonBuilding.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "name":
                        existingBuilding.Name = prop.Value.GetString() ?? "";
                        break;

                    case "description":
                        existingBuilding.Description = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                        break;
                }
            }

            existingBuilding.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Building
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Building>> PostBuilding(Building building)
        {
            // Set timestamps
            building.CreatedAt = DateTime.UtcNow;
            building.UpdatedAt = DateTime.UtcNow;

            context.Buildings.Add(building);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetBuilding", new { id = building.Id }, building);
        }

        // DELETE: api/Building/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBuilding(int id)
        {
            var building = await context.Buildings.FindAsync(id);
            if (building == null)
            {
                return NotFound();
            }

            context.Buildings.Remove(building);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool BuildingExists(int id)
        {
            return context.Buildings.Any(e => e.Id == id);
        }
    }
}
