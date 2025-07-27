using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoiPointController(MyDbContext context) : ControllerBase
    {
        // GET: api/PoiPoint
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PoiPoint>>> GetPoiPoints()
        {
            return await context.PoiPoints
                .OrderBy(pp => pp.PoiId)
                .ThenBy(pp => pp.PointOrder)
                .ToListAsync();
        }

        // GET: api/PoiPoint/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PoiPoint>> GetPoiPoint(int id)
        {
            var poiPoint = await context.PoiPoints.FindAsync(id);

            if (poiPoint == null)
            {
                return NotFound();
            }

            return poiPoint;
        }

        // GET: api/PoiPoint/poi/5
        [HttpGet("poi/{poiId}")]
        public async Task<ActionResult<IEnumerable<PoiPoint>>> GetPoiPointsByPoiId(int poiId)
        {
            return await context.PoiPoints
                .Where(pp => pp.PoiId == poiId)
                .OrderBy(pp => pp.PointOrder)
                .ToListAsync();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoiPoint(int id, JsonElement jsonPoiPoint)
        {
            var existingPoiPoint = await context.PoiPoints.FindAsync(id);
            if (existingPoiPoint == null)
                return NotFound();

            foreach (var prop in jsonPoiPoint.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "poiid":
                        if (prop.Value.TryGetInt32(out var poiId))
                            existingPoiPoint.PoiId = poiId;
                        break;

                    case "x":
                        if (prop.Value.TryGetDecimal(out var x))
                            existingPoiPoint.X = x;
                        break;

                    case "y":
                        if (prop.Value.TryGetDecimal(out var y))
                            existingPoiPoint.Y = y;
                        break;

                    case "pointorder":
                        if (prop.Value.TryGetInt32(out var order))
                            existingPoiPoint.PointOrder = order;
                        break;
                }
            }

            existingPoiPoint.CreatedAt = existingPoiPoint.CreatedAt; // keep original createdAt unchanged

            // You might want to add an UpdatedAt property to PoiPoint for consistency? If so, update here.
            // For now, just save changes:
            await context.SaveChangesAsync();
            return NoContent();
        }


        // POST: api/PoiPoint
        [HttpPost]
        public async Task<ActionResult<PoiPoint>> PostPoiPoint(PoiPoint poiPoint)
        {
            // Set timestamp
            poiPoint.CreatedAt = DateTime.UtcNow;

            context.PoiPoints.Add(poiPoint);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetPoiPoint", new { id = poiPoint.Id }, poiPoint);
        }

        // POST: api/PoiPoint/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<PoiPoint>>> PostPoiPointsBulk(IEnumerable<PoiPoint> poiPoints)
        {
            var pointList = poiPoints.ToList();
            
            // Set timestamps for all points
            foreach (var point in pointList)
            {
                point.CreatedAt = DateTime.UtcNow;
            }

            context.PoiPoints.AddRange(pointList);
            await context.SaveChangesAsync();

            return Ok(pointList);
        }

        // DELETE: api/PoiPoint/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoiPoint(int id)
        {
            var poiPoint = await context.PoiPoints.FindAsync(id);
            if (poiPoint == null)
            {
                return NotFound();
            }

            context.PoiPoints.Remove(poiPoint);
            await context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/PoiPoint/poi/5
        [HttpDelete("poi/{poiId}")]
        public async Task<IActionResult> DeletePoiPointsByPoiId(int poiId)
        {
            var poiPoints = await context.PoiPoints
                .Where(pp => pp.PoiId == poiId)
                .ToListAsync();

            if (poiPoints.Count == 0)
            {
                return NotFound();
            }

            context.PoiPoints.RemoveRange(poiPoints);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool PoiPointExists(int id)
        {
            return context.PoiPoints.Any(e => e.Id == id);
        }
    }
} 