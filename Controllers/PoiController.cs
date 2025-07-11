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
            return await context.Pois.ToListAsync();
        }

        // GET: api/Poi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Poi>> GetPoi(int id)
        {
            var poi = await context.Pois.FindAsync(id);

            if (poi == null)
            {
                return NotFound();
            }

            return poi;
        }

        // PUT: api/Poi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoi(int id, Poi poi)
        {
            if (id != poi.Id)
            {
                return BadRequest();
            }

            context.Entry(poi).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PoiExists(id))
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

        // POST: api/Poi
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Poi>> PostPoi(Poi poi)
        {
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
