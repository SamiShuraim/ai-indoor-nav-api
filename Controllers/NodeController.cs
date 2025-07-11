using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NodeController(MyDbContext context) : ControllerBase
    {
        // GET: api/Node
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Node>>> GetNodes()
        {
            return await context.Nodes.ToListAsync();
        }

        // GET: api/Node/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Node>> GetNode(int id)
        {
            var node = await context.Nodes.FindAsync(id);

            if (node == null)
            {
                return NotFound();
            }

            return node;
        }

        // PUT: api/Node/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNode(int id, Node node)
        {
            if (id != node.Id)
            {
                return BadRequest();
            }

            context.Entry(node).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NodeExists(id))
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

        // POST: api/Node
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Node>> PostNode(Node node)
        {
            context.Nodes.Add(node);
            await context.SaveChangesAsync();

            return CreatedAtAction("GetNode", new { id = node.Id }, node);
        }

        // DELETE: api/Node/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            var node = await context.Nodes.FindAsync(id);
            if (node == null)
            {
                return NotFound();
            }

            context.Nodes.Remove(node);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool NodeExists(int id)
        {
            return context.Nodes.Any(e => e.Id == id);
        }
    }
}
