using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WallPointController(MyDbContext context) : ControllerBase
    {
        // GET: api/WallPoint
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WallPoint>>> GetWallPoints()
        {
            return await context.WallPoints
                .Include(wp => wp.Wall)
                .OrderBy(wp => wp.WallId)
                .ThenBy(wp => wp.PointOrder)
                .ToListAsync();
        }

        // GET: api/WallPoint/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WallPoint>> GetWallPoint(int id)
        {
            var wallPoint = await context.WallPoints
                .Include(wp => wp.Wall)
                .FirstOrDefaultAsync(wp => wp.Id == id);

            if (wallPoint == null)
            {
                return NotFound();
            }

            return wallPoint;
        }

        // GET: api/WallPoint/wall/5
        [HttpGet("wall/{wallId}")]
        public async Task<ActionResult<IEnumerable<WallPoint>>> GetWallPointsByWallId(int wallId)
        {
            return await context.WallPoints
                .Where(wp => wp.WallId == wallId)
                .Include(wp => wp.Wall)
                .OrderBy(wp => wp.PointOrder)
                .ToListAsync();
        }

        // PUT: api/WallPoint/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWallPoint(int id, JsonElement jsonWallPoint)
        {
            var existingPoint = await context.WallPoints.FindAsync(id);
            if (existingPoint == null)
                return NotFound();

            foreach (var prop in jsonWallPoint.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "wallid":
                        if (prop.Value.TryGetInt32(out var wallId))
                            existingPoint.WallId = wallId;
                        break;

                    case "x":
                        if (prop.Value.TryGetDecimal(out var x))
                            existingPoint.X = x;
                        break;

                    case "y":
                        if (prop.Value.TryGetDecimal(out var y))
                            existingPoint.Y = y;
                        break;

                    case "pointorder":
                        if (prop.Value.TryGetInt32(out var order))
                            existingPoint.PointOrder = order;
                        break;
                }
            }

            // createdAt is not updated
            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/WallPoint
        [HttpPost]
        public async Task<ActionResult<WallPoint>> PostWallPoint(WallPoint wallPoint)
        {
            // Set timestamp
            wallPoint.CreatedAt = DateTime.UtcNow;

            context.WallPoints.Add(wallPoint);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetWallPoint", new { id = wallPoint.Id }, wallPoint);
        }

        // POST: api/WallPoint/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<WallPoint>>> PostWallPointsBulk(IEnumerable<WallPoint> wallPoints)
        {
            var pointList = wallPoints.ToList();
            
            // Set timestamps for all points
            foreach (var point in pointList)
            {
                point.CreatedAt = DateTime.UtcNow;
            }

            context.WallPoints.AddRange(pointList);
            await context.SaveChangesAsync();

            return Ok(pointList);
        }

        // DELETE: api/WallPoint/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWallPoint(int id)
        {
            var wallPoint = await context.WallPoints.FindAsync(id);
            if (wallPoint == null)
            {
                return NotFound();
            }

            context.WallPoints.Remove(wallPoint);
            await context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/WallPoint/wall/5
        [HttpDelete("wall/{wallId}")]
        public async Task<IActionResult> DeleteWallPointsByWallId(int wallId)
        {
            var wallPoints = await context.WallPoints
                .Where(wp => wp.WallId == wallId)
                .ToListAsync();

            if (wallPoints.Count == 0)
            {
                return NotFound();
            }

            context.WallPoints.RemoveRange(wallPoints);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool WallPointExists(int id)
        {
            return context.WallPoints.Any(e => e.Id == id);
        }
    }
} 