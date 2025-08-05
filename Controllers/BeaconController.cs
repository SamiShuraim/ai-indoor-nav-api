using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BeaconController(MyDbContext context) : ControllerBase
    {
        // GET: api/Beacon
        [HttpGet]
        public async Task<ActionResult<FeatureCollection>> GetBeacons([FromQuery] int? floor, [FromQuery] int? building)
        {
            var query = context.Beacons
                .Include(b => b.BeaconType)
                .Include(b => b.Floor)
                .AsQueryable();

            if (floor.HasValue)
                query = query.Where(b => b.FloorId == floor.Value);

            if (building.HasValue)
                query = query.Where(b => b.Floor != null && b.Floor.BuildingId == building.Value);

            var beacons = await query.ToListAsync();

            return Ok(beacons.ToGeoJsonFeatureCollection()); // use ToList() not ToListAsync()
        }

        // GET: api/Beacon/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Feature>> GetBeacon(int id)
        {
            var beacon = await context.Beacons
                .Include(b => b.BeaconType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (beacon == null)
            {
                return NotFound();
            }

            return Ok(beacon.ToGeoJsonFeature());
        }

        // GET: api/Beacon/floor/5
        [HttpGet("floor/{floorId}")]
        public async Task<ActionResult<FeatureCollection>> GetBeaconsByFloorId(int floorId)
        {
            var query = await context.Beacons
                .Where(b => b.FloorId == floorId)
                .Include(b => b.BeaconType).ToListAsync();
            
            return Ok(query.ToGeoJsonFeatureCollection());
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

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBeacon(int id, JsonElement jsonBeacon)
        {
            var existingBeacon = await context.Beacons.FindAsync(id);
            if (existingBeacon == null)
                return NotFound();

            existingBeacon.PopulateFromJson(jsonBeacon);

            existingBeacon.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
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
        public async Task<ActionResult<Feature>> PostBeacon(Beacon beacon)
        {
            context.Beacons.Add(beacon);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetBeacon", new { id = beacon.Id }, beacon.ToGeoJsonFeature());
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