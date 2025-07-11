using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EdgeController(MyDbContext context) : ControllerBase
    {
        // GET: api/Edge
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Edge>>> GetEdges()
        {
            return await context.Edges.ToListAsync();
        }

        // GET: api/Edge/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Edge>> GetEdge(int id)
        {
            var edge = await context.Edges.FindAsync(id);

            if (edge == null)
            {
                return NotFound();
            }

            return edge;
        }

        // PUT: api/Edge/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEdge(int id, Edge edge)
        {
            if (id != edge.Id)
            {
                return BadRequest();
            }

            context.Entry(edge).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EdgeExists(id))
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

        // POST: api/Edge
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Edge>> PostEdge(Edge edge)
        {
            context.Edges.Add(edge);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetEdge", new { id = edge.Id }, edge);
        }

        // DELETE: api/Edge/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEdge(int id)
        {
            var edge = await context.Edges.FindAsync(id);
            if (edge == null)
            {
                return NotFound();
            }

            context.Edges.Remove(edge);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool EdgeExists(int id)
        {
            return context.Edges.Any(e => e.Id == id);
        }
    }
}
