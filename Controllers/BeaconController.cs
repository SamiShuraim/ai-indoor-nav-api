using System.Text.Json;
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

        [HttpPut("{id}")]
        public async Task<IActionResult> PutBeacon(int id, JsonElement jsonBeacon)
        {
            var existingBeacon = await context.Beacons.FindAsync(id);
            if (existingBeacon == null)
                return NotFound();

            foreach (var prop in jsonBeacon.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "floorid":
                        if (prop.Value.TryGetInt32(out var floorId))
                            existingBeacon.FloorId = floorId;
                        break;

                    case "beacontypeid":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetInt32(out var beaconTypeId))
                            existingBeacon.BeaconTypeId = beaconTypeId;
                        else
                            existingBeacon.BeaconTypeId = null;
                        break;

                    case "name":
                        existingBeacon.Name = prop.Value.GetString() ?? "";
                        break;

                    case "uuid":
                        existingBeacon.Uuid = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                        break;

                    case "majorid":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetInt32(out var majorId))
                            existingBeacon.MajorId = majorId;
                        else
                            existingBeacon.MajorId = null;
                        break;

                    case "minorid":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetInt32(out var minorId))
                            existingBeacon.MinorId = minorId;
                        else
                            existingBeacon.MinorId = null;
                        break;

                    case "x":
                        if (prop.Value.TryGetDecimal(out var x))
                            existingBeacon.X = x;
                        break;

                    case "y":
                        if (prop.Value.TryGetDecimal(out var y))
                            existingBeacon.Y = y;
                        break;

                    case "z":
                        if (prop.Value.TryGetDecimal(out var z))
                            existingBeacon.Z = z;
                        break;

                    case "isactive":
                        if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                            existingBeacon.IsActive = prop.Value.GetBoolean();
                        break;

                    case "isvisible":
                        if (prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False)
                            existingBeacon.IsVisible = prop.Value.GetBoolean();
                        break;

                    case "batterylevel":
                        if (prop.Value.TryGetInt32(out var batteryLevel))
                            existingBeacon.BatteryLevel = batteryLevel;
                        break;

                    case "lastseen":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetDateTime(out var lastSeen))
                            existingBeacon.LastSeen = lastSeen;
                        else
                            existingBeacon.LastSeen = null;
                        break;

                    case "installationdate":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetDateTime(out var installDate))
                            existingBeacon.InstallationDate = installDate;
                        else
                            existingBeacon.InstallationDate = null;
                        break;
                }
            }

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