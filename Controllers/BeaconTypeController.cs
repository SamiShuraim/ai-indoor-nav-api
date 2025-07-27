using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BeaconTypeController(MyDbContext context) : ControllerBase
    {
        // GET: api/BeaconType
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BeaconType>>> GetBeaconTypes()
        {
            return await context.BeaconTypes
                .Include(bt => bt.Beacons)
                .ToListAsync();
        }

        // GET: api/BeaconType/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BeaconType>> GetBeaconType(int id)
        {
            var beaconType = await context.BeaconTypes
                .Include(bt => bt.Beacons)
                .FirstOrDefaultAsync(bt => bt.Id == id);

            if (beaconType == null)
            {
                return NotFound();
            }

            return beaconType;
        }

        // GET: api/BeaconType/name/{name}
        [HttpGet("name/{name}")]
        public async Task<ActionResult<BeaconType>> GetBeaconTypeByName(string name)
        {
            var beaconType = await context.BeaconTypes
                .Include(bt => bt.Beacons)
                .FirstOrDefaultAsync(bt => bt.Name == name);

            if (beaconType == null)
            {
                return NotFound();
            }

            return beaconType;
        }

        // PUT: api/BeaconType/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBeaconType(int id, JsonElement jsonBeaconType)
        {
            var existingType = await context.BeaconTypes.FindAsync(id);
            if (existingType == null)
                return NotFound();

            foreach (var prop in jsonBeaconType.EnumerateObject())
            {
                switch (prop.Name.ToLower())
                {
                    case "name":
                        existingType.Name = prop.Value.GetString() ?? "";
                        break;

                    case "description":
                        existingType.Description = prop.Value.ValueKind == JsonValueKind.Null ? null : prop.Value.GetString();
                        break;

                    case "transmissionpower":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetInt32(out var power))
                            existingType.TransmissionPower = power;
                        else
                            existingType.TransmissionPower = null;
                        break;

                    case "batterylife":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetInt32(out var batteryLife))
                            existingType.BatteryLife = batteryLife;
                        else
                            existingType.BatteryLife = null;
                        break;

                    case "rangemeters":
                        if (prop.Value.ValueKind != JsonValueKind.Null && prop.Value.TryGetDecimal(out var range))
                            existingType.RangeMeters = range;
                        else
                            existingType.RangeMeters = null;
                        break;
                }
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/BeaconType
        [HttpPost]
        public async Task<ActionResult<BeaconType>> PostBeaconType(BeaconType beaconType)
        {
            context.BeaconTypes.Add(beaconType);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetBeaconType", new { id = beaconType.Id }, beaconType);
        }

        // DELETE: api/BeaconType/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBeaconType(int id)
        {
            var beaconType = await context.BeaconTypes.FindAsync(id);
            if (beaconType == null)
            {
                return NotFound();
            }

            context.BeaconTypes.Remove(beaconType);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool BeaconTypeExists(int id)
        {
            return context.BeaconTypes.Any(e => e.Id == id);
        }
    }
} 