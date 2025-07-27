using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiCategoryController(MyDbContext context) : ControllerBase
    {
        // GET: api/PoiCategory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PoiCategory>>> GetPoiCategories()
        {
            return await context.PoiCategories.ToListAsync();
        }

        // GET: api/PoiCategory/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PoiCategory>> GetPoiCategory(int id)
        {
            var poiCategory = await context.PoiCategories.FindAsync(id);

            if (poiCategory == null)
            {
                return NotFound();
            }

            return poiCategory;
        }

        // GET: api/PoiCategory/name/{name}
        [HttpGet("name/{name}")]
        public async Task<ActionResult<PoiCategory>> GetPoiCategoryByName(string name)
        {
            var poiCategory = await context.PoiCategories
                .FirstOrDefaultAsync(pc => pc.Name == name);

            if (poiCategory == null)
            {
                return NotFound();
            }

            return poiCategory;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoiCategory(int id, JsonElement jsonPoiCategory)
        {
            var existingCategory = await context.PoiCategories.FindAsync(id);
            if (existingCategory == null)
                return NotFound();

            foreach (var prop in jsonPoiCategory.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "name":
                        existingCategory.Name = prop.Value.GetString() ?? "";
                        break;

                    case "color":
                        existingCategory.Color = prop.Value.GetString() ?? "#3B82F6";
                        break;

                    case "description":
                        existingCategory.Description = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                        break;
                }
            }

            // No UpdatedAt property in this model, add if needed.

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/PoiCategory
        [HttpPost]
        public async Task<ActionResult<PoiCategory>> PostPoiCategory(PoiCategory poiCategory)
        {
            context.PoiCategories.Add(poiCategory);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetPoiCategory", new { id = poiCategory.Id }, poiCategory);
        }

        // DELETE: api/PoiCategory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoiCategory(int id)
        {
            var poiCategory = await context.PoiCategories.FindAsync(id);
            if (poiCategory == null)
            {
                return NotFound();
            }

            context.PoiCategories.Remove(poiCategory);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool PoiCategoryExists(int id)
        {
            return context.PoiCategories.Any(e => e.Id == id);
        }
    }
} 