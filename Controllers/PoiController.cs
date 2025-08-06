using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Enums;
using ai_indoor_nav_api.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using static System.DateTime;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiController(MyDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<FeatureCollection>> GetPois([FromQuery] int? floor, [FromQuery] int? building)
        {
            var query = context.Pois.Include(p => p.Floor).AsQueryable();
            
            if (floor.HasValue)
            {
                query = query.Where(p => p.FloorId == floor.Value);
            }

            if (building.HasValue)
            {
                query = query.Where(p => p.Floor!.BuildingId == building.Value);
            }

            var pois = await query.ToListAsync();
           
            return Ok(pois.ToGeoJsonFeatureCollection());
        }
        
        // GET: api/Poi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPoi(int id)
        {
            var poi = await context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            return Ok(poi.ToGeoJsonFeature());
        }

        // PUT: api/Poi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoi(int id, JsonElement jsonBeacon)
        {
            var existingPoi = await context.Pois.FindAsync(id);
            if (existingPoi == null)
                return NotFound();

            existingPoi.PopulateFromJson(jsonBeacon);

            existingPoi.UpdatedAt = UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Poi
        [HttpPost]
        public async Task<ActionResult<Feature>> PostPoi()
        {
            var (success, errorMessage, poi) = await RequestParser.TryParseFlattenedEntity<Poi>(Request);

            if (!success)
                return BadRequest(errorMessage);
            
            if (poi == null)
                return BadRequest(errorMessage);

            context.Pois.Add(poi);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPoi), new { id = poi.Id }, poi.ToGeoJsonFeature());
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
