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

        // PUT: api/PoiPoint/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPoiPoint(int id, PoiPoint poiPoint)
        {
            if (id != poiPoint.Id)
            {
                return BadRequest();
            }

            // Update the timestamp
            poiPoint.CreatedAt = DateTime.UtcNow;

            context.Entry(poiPoint).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PoiPointExists(id))
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