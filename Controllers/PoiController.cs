using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiController(MyDbContext context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPois()
        {
            var pois = await context.Pois.ToListAsync();
            var writer = new GeoJsonWriter();

            var features = pois.Select(poi => new
            {
                type = "Feature",
                geometry = JsonConvert.DeserializeObject(writer.Write(poi.Geometry)),
                properties = new
                {
                    poi.Id,
                    poi.Name,
                    poi.FloorId,
                    poi.CategoryId,
                    poi.Description,
                    poi.PoiType,
                    poi.Color,
                    poi.IsVisible,
                    poi.CreatedAt,
                    poi.UpdatedAt
                }
            });

            return Ok(features);
        }


        // GET: api/Poi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPoi(int id)
        {
            var poi = await context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            var writer = new GeoJsonWriter();
            var geometryJson = writer.Write(poi.Geometry);

            var feature = new
            {
                type = "Feature",
                geometry = JsonConvert.DeserializeObject(geometryJson),
                properties = new
                {
                    poi.Name,
                    poi.FloorId,
                    poi.CategoryId,
                    poi.Description,
                    poi.PoiType,
                    poi.Color,
                    poi.IsVisible
                }
            };

            return Ok(feature);
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
        [HttpPost]
        public async Task<IActionResult> CreatePoi([FromBody] GeoJsonFeatureDto input)
        {
            var reader = new GeoJsonReader();

            // Reconstruct full GeoJSON string
            var geoJson = JsonConvert.SerializeObject(new
            {
                type = "Feature",
                geometry = input.Geometry
            });

            Geometry geometry;
            try
            {
                geometry = reader.Read<Geometry>(geoJson);
            }
            catch (Exception ex)
            {
                return BadRequest($"Invalid geometry: {ex.Message}");
            }

            var poi = new Poi
            {
                Name = input.Properties.Name,
                FloorId = input.Properties.FloorId,
                CategoryId = input.Properties.CategoryId,
                Description = input.Properties.Description,
                PoiType = input.Properties.PoiType,
                Color = input.Properties.Color,
                IsVisible = input.Properties.IsVisible,
                Geometry = geometry,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Pois.Add(poi);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPoi), new { id = poi.Id }, poi);
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
