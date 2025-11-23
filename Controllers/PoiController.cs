using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Enums;
using ai_indoor_nav_api.Filters;
using ai_indoor_nav_api.Models;
using ai_indoor_nav_api.Services;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using static System.DateTime;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiController(MyDbContext context, NavigationService navigationService) : ControllerBase
    {
        [HttpGet]
        [HttpCache(Duration = 300, VaryByQuery = true)]
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
        [HttpCache(Duration = 300)]
        public async Task<IActionResult> GetPoi(int id)
        {
            var poi = await context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            return Ok(poi.ToGeoJsonFeature());
        }

        // PUT: api/Poi/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoi(int id)
        {
            var existingPoi = await context.Pois.FindAsync(id);
            if (existingPoi == null)
                return NotFound();

            var (success, errorMessage, updatedPoi) =
                await RequestParser.TryParseFlattenedEntity<Poi>(Request);

            if (!success || updatedPoi == null)
                return BadRequest(errorMessage);
            
            Console.WriteLine($"Old Name: {existingPoi.Name}, New Name: {updatedPoi.Name}");

            // Update only the fields you want to allow editing
            existingPoi.Name = updatedPoi.Name;
            existingPoi.Category = updatedPoi.Category;
            existingPoi.PoiType = updatedPoi.PoiType;
            existingPoi.CategoryId = updatedPoi.CategoryId;
            existingPoi.Color = updatedPoi.Color;
            existingPoi.Description = updatedPoi.Description;
            
            // existingBeacon.Geometry = updatedBeacon.Geometry; // if you store geometry as a type

            existingPoi.UpdatedAt = DateTime.UtcNow;

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

        // POST: api/Poi/recalculateClosestNodes
        [HttpPost("recalculateClosestNodes")]
        public async Task<IActionResult> RecalculateClosestNodes([FromQuery] int? floorId = null)
        {
            try
            {
                Console.WriteLine($"[POI_CONTROLLER] Starting POI closest nodes recalculation for {(floorId.HasValue ? $"floor {floorId}" : "all floors")}");
                
                // Validate floor exists if specified
                if (floorId.HasValue)
                {
                    var floorExists = await context.Floors.AnyAsync(f => f.Id == floorId.Value);
                    if (!floorExists)
                    {
                        return BadRequest($"Floor with ID {floorId} does not exist");
                    }
                }

                var (updatedPois, report) = await navigationService.RecalculateAllPoiClosestNodesAsync(floorId);
                
                Console.WriteLine($"[POI_CONTROLLER] Recalculation completed. Updated {updatedPois} POIs");

                return Ok(new
                {
                    success = true,
                    updatedPois = updatedPois,
                    floorId = floorId,
                    message = floorId.HasValue 
                        ? $"Successfully recalculated closest nodes for {updatedPois} POIs on floor {floorId}"
                        : $"Successfully recalculated closest nodes for {updatedPois} POIs across all floors",
                    report = report
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POI_CONTROLLER] Error during POI closest nodes recalculation: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    error = "An error occurred while recalculating POI closest nodes",
                    details = ex.Message
                });
            }
        }

        private bool PoiExists(int id)
        {
            return context.Pois.Any(e => e.Id == id);
        }
    }
}
