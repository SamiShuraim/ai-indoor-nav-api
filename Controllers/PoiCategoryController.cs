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

        // PUT: api/PoiCategory/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoiCategory(int id, PoiCategory poiCategory)
        {
            if (id != poiCategory.Id)
            {
                return BadRequest();
            }

            context.Entry(poiCategory).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PoiCategoryExists(id))
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