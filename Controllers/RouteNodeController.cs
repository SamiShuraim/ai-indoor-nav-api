using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;
using NetTopologySuite.Geometries;
using static System.DateTime;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteNodeController(MyDbContext context) : ControllerBase
    {
        // GET: api/RouteNode
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RouteNode>>> GetRouteNodes()
        {
            return await context.RouteNodes.ToListAsync();
        }

        // GET: api/RouteNode/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RouteNode>> GetRouteNode(int id)
        {
            var routeNode = await context.RouteNodes.FindAsync(id);

            if (routeNode == null)
            {
                return NotFound();
            }

            return routeNode;
        }

        // PUT: api/RouteNode/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRouteNode(int id, RouteNodeDto dto)
        {
            var node = await context.RouteNodes.FindAsync(id);
            if (node == null) return NotFound();

            node.FloorId = dto.FloorId;
            node.Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 };
            node.IsVisible = dto.IsVisible;
            node.ConnectedNodeIds = dto.ConnectedNodeIds;
            node.UpdatedAt = UtcNow;

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/RouteNode
        [HttpPost]
        public async Task<ActionResult<RouteNode>> CreateRouteNode(RouteNodeDto dto)
        {
            var node = new RouteNode
            {
                FloorId = dto.FloorId,
                Location = new Point(dto.Longitude, dto.Latitude) { SRID = 4326 },
                IsVisible = dto.IsVisible,
                ConnectedNodeIds = dto.ConnectedNodeIds,
                CreatedAt = UtcNow,
                UpdatedAt = UtcNow
            };

            context.RouteNodes.Add(node);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRouteNode), new { id = node.Id }, node);
        }

        // DELETE: api/RouteNode/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteNode(int id)
        {
            var routeNode = await context.RouteNodes.FindAsync(id);
            if (routeNode == null)
            {
                return NotFound();
            }

            context.RouteNodes.Remove(routeNode);
            await context.SaveChangesAsync();

            return NoContent();
        }

        private bool RouteNodeExists(int id)
        {
            return context.RouteNodes.Any(e => e.Id == id);
        }
    }
}