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
        public async Task<IActionResult> PutBeaconType(int id, BeaconType beaconType)
        {
            if (id != beaconType.Id)
            {
                return BadRequest();
            }

            context.Entry(beaconType).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BeaconTypeExists(id))
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