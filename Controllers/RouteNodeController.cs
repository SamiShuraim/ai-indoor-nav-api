using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using static System.DateTime;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteNodeController(MyDbContext context) : ControllerBase
    {
        // GET: api/RouteNode?floor=1&building=3
        [HttpGet]
        public async Task<ActionResult<FeatureCollection>> GetRouteNodes([FromQuery] int? floor, [FromQuery] int? building)
        {
            var query = context.RouteNodes.AsQueryable();

            if (floor.HasValue)
            {
                query = query.Where(rn => rn.FloorId == floor.Value);
            }

            if (building.HasValue)
            {
                query = query.Where(rn => rn.Floor!.BuildingId == building.Value);
            }

            return Ok(query.ToGeoJsonFeatureCollection());
        }


        // GET: api/RouteNode/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Feature>> GetRouteNode(int id)
        {
            var routeNode = await context.RouteNodes.FindAsync(id);

            if (routeNode == null)
            {
                return NotFound();
            }

            return Ok(routeNode.ToGeoJsonFeature());
        }

        // PUT: api/RouteNode/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRouteNode(int id, JsonElement jsonElement)
        {
            var node = await context.RouteNodes.FindAsync(id);
            if (node == null) return NotFound();

            node.PopulateFromJson(jsonElement);

            await context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/RouteNode
        [HttpPost]
        public async Task<ActionResult<RouteNode>> CreateRouteNode()
        {
            var (success, errorMessage, node) = 
                await RequestParser.TryParseFlattenedEntity<RouteNode>(Request);

            if (!success)
                return BadRequest(errorMessage);

            if (node == null)
                return BadRequest(errorMessage);

            // Validate foreign keys early to avoid 500s from the database layer
            var floorExists = await context.Floors.AnyAsync(f => f.Id == node.FloorId);
            if (!floorExists)
            {
                return BadRequest($"Invalid floor_id {node.FloorId}: floor does not exist");
            }

            context.RouteNodes.Add(node);
            await context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRouteNode), new { id = node.Id }, node.ToGeoJsonFeature());
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