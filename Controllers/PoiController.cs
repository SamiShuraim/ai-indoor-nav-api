using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiController(MyDbContext context) : ControllerBase
    {
        // GET: api/Poi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Poi>>> GetPois()
        {
            return await context.Pois
                .Include(p => p.Category)
                .Include(p => p.PoiPoints)
                .ToListAsync();
        }

        // GET: api/Poi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Poi>> GetPoi(int id)
        {
            var poi = await context.Pois
                .Include(p => p.Category)
                .Include(p => p.PoiPoints)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (poi == null)
            {
                return NotFound();
            }

            return poi;
        }

        // GET: api/Poi/floor/5
        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<IEnumerable<Poi>>> GetPoisByFloorId(int floorId)
        {
            return await context.Pois
                .Where(p => p.FloorId == floorId)
                .Include(p => p.Category)
                .Include(p => p.PoiPoints)
                .ToListAsync();
        }

        // PUT: api/Poi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoi(int id, JsonElement jsonPoi)
        {
            var existingPoi = await context.Pois.FindAsync(id);
            if (existingPoi == null)
                return NotFound();

            foreach (var prop in jsonPoi.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "floorid":
                        if (prop.Value.TryGetInt32(out var floorId))
                            existingPoi.FloorId = floorId;
                        break;

                    case "categoryid":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetInt32(out var categoryId))
                            existingPoi.CategoryId = categoryId;
                        else
                            existingPoi.CategoryId = null;
                        break;

                    case "name":
                        existingPoi.Name = prop.Value.GetString() ?? "";
                        break;

                    case "description":
                        existingPoi.Description = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                        break;

                    case "poitype":
                        existingPoi.PoiType = prop.Value.GetString() ?? "room";
                        break;

                    case "color":
                        existingPoi.Color = prop.Value.GetString() ?? "#3B82F6";
                        break;

                    case "isvisible":
                        if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                            existingPoi.IsVisible = prop.Value.GetBoolean();
                        break;
                }
            }

            existingPoi.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }


        // POST: api/Poi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Poi>> PostPoi(Poi poi)
        {
            // Set timestamps
            poi.CreatedAt = DateTime.UtcNow;
            poi.UpdatedAt = DateTime.UtcNow;

            context.Pois.Add(poi);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetPoi", new { id = poi.Id }, poi);
        }

        // DELETE: api/Poi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoi(int id)
        {
            var poi = await context.Pois.FindAsync(id);
            if (poi == null)
            {
                return NotFound();
            }

            context.Pois.Remove(poi);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool PoiExists(int id)
        {
            return context.Pois.Any(e => e.Id == id);
        }
    }
}
