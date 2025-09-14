using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task<IActionResult> UpdateRouteNode(int id)
        {
            var node = await context.RouteNodes.FindAsync(id);
            if (node == null) return NotFound();

            try
            {
                // Read the JSON from the request body
                using var reader = new StreamReader(Request.Body);
                var jsonString = await reader.ReadToEndAsync();
                
                if (string.IsNullOrEmpty(jsonString))
                {
                    return BadRequest("Request body is empty.");
                }

                using var jsonDocument = JsonDocument.Parse(jsonString);
                var jsonElement = jsonDocument.RootElement;

                // Check if this is a valid JSON object before trying to access properties
                if (jsonElement.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest($"Invalid JSON: Expected an object, got {jsonElement.ValueKind}.");
                }

                // Check if this is a GeoJSON Feature object
                if (jsonElement.TryGetProperty("type", out var typeProperty) && 
                    typeProperty.ValueKind == JsonValueKind.String &&
                    typeProperty.GetString() == "Feature")
                {
                    // Handle GeoJSON Feature object
                    if (jsonElement.TryGetProperty("properties", out var propertiesElement))
                    {
                        node.PopulateFromJson(propertiesElement);
                    }

                    // Handle geometry
                    if (jsonElement.TryGetProperty("geometry", out var geometryElement))
                    {
                        node.UpdateGeometryFromJson(geometryElement);
                    }
                }
                else
                {
                    // Handle flat JSON object (backward compatibility)
                    node.PopulateFromJson(jsonElement);
                }

                await context.SaveChangesAsync();
                return NoContent();
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"JSON parsing error: {ex.Message}");
            }
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