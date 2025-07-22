using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BeaconController(MyDbContext context) : ControllerBase
    {
        // GET: api/Beacon
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Beacon>>> GetBeacons()
        {
            return await context.Beacons
                .Include(b => b.BeaconType)
                .ToListAsync();
        }

        // GET: api/Beacon/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Beacon>> GetBeacon(int id)
        {
            var beacon = await context.Beacons
                .Include(b => b.BeaconType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (beacon == null)
            {
                return NotFound();
            }

            return beacon;
        }

        // GET: api/Beacon/floor/5
        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<IEnumerable<Beacon>>> GetBeaconsByFloorId(int floorId)
        {
            return await context.Beacons
                .Where(b => b.FloorId == floorId)
                .Include(b => b.BeaconType)
                .ToListAsync();
        }

        // GET: api/Beacon/uuid/{uuid}
        [HttpGet("uuid/{uuid}")]
        public async Task<ActionResult<IEnumerable<Beacon>>> GetBeaconsByUuid(string uuid)
        {
            return await context.Beacons
                .Where(b => b.Uuid == uuid)
                .Include(b => b.BeaconType)
                .ToListAsync();
        }

        // GET: api/Beacon/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Beacon>>> GetActiveBeacons()
        {
            return await context.Beacons
                .Where(b => b.IsActive)
                .Include(b => b.BeaconType)
                .ToListAsync();
        }

        // GET: api/Beacon/low-battery/{threshold}
        [HttpGet("low-battery/{threshold}")]
        public async Task<ActionResult<IEnumerable<Beacon>>> GetLowBatteryBeacons(int threshold = 20)
        {
            return await context.Beacons
                .Where(b => b.BatteryLevel <= threshold && b.IsActive)
                .Include(b => b.BeaconType)
                .OrderBy(b => b.BatteryLevel)
                .ToListAsync();
        }

        // PUT: api/Beacon/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBeacon(int id, Beacon beacon)
        {
            if (id != beacon.Id)
            {
                return BadRequest();
            }

            // Update the UpdatedAt timestamp
            beacon.UpdatedAt = DateTime.UtcNow;

            context.Entry(beacon).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BeaconExists(id))
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

        // PUT: api/Beacon/5/battery/{level}
        [HttpPut("{id}/battery/{level}")]
        public async Task<IActionResult> UpdateBeaconBatteryLevel(int id, int level)
        {
            var beacon = await context.Beacons.FindAsync(id);
            if (beacon == null)
            {
                return NotFound();
            }

            beacon.BatteryLevel = level;
            beacon.UpdatedAt = DateTime.UtcNow;
            beacon.LastSeen = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/Beacon/5/heartbeat
        [HttpPut("{id}/heartbeat")]
        public async Task<IActionResult> UpdateBeaconHeartbeat(int id)
        {
            var beacon = await context.Beacons.FindAsync(id);
            if (beacon == null)
            {
                return NotFound();
            }

            beacon.LastSeen = DateTime.UtcNow;
            beacon.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Beacon
        [HttpPost]
        public async Task<ActionResult<Beacon>> PostBeacon(Beacon beacon)
        {
            // Set timestamps
            beacon.CreatedAt = DateTime.UtcNow;
            beacon.UpdatedAt = DateTime.UtcNow;

            context.Beacons.Add(beacon);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetBeacon", new { id = beacon.Id }, beacon);
        }

        // DELETE: api/Beacon/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBeacon(int id)
        {
            var beacon = await context.Beacons.FindAsync(id);
            if (beacon == null)
            {
                return NotFound();
            }

            context.Beacons.Remove(beacon);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool BeaconExists(int id)
        {
            return context.Beacons.Any(e => e.Id == id);
        }
    }
} 