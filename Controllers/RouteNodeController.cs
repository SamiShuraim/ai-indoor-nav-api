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
using ai_indoor_nav_api.Services;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using static System.DateTime;

namespace ai_indoor_nav_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouteNodeController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly NavigationService _navigationService;
        private readonly NodeCacheService _cacheService;
        private readonly ConnectionPointDetectionService _connectionDetectionService;

        public RouteNodeController(
            MyDbContext context, 
            NavigationService navigationService,
            NodeCacheService cacheService,
            ConnectionPointDetectionService connectionDetectionService)
        {
            _context = context;
            _navigationService = navigationService;
            _cacheService = cacheService;
            _connectionDetectionService = connectionDetectionService;
        }
        // GET: api/RouteNode?floor=1&building=3
        [HttpGet]
        public async Task<ActionResult<FeatureCollection>> GetRouteNodes([FromQuery] int? floor, [FromQuery] int? building)
        {
            var query = _context.RouteNodes.AsQueryable();

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
            // Use cache for faster lookups (especially under high concurrency)
            var routeNode = await _cacheService.GetNodeByIdAsync(id);

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
            var node = await _context.RouteNodes.FindAsync(id);
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

                await _context.SaveChangesAsync();
                
                // Invalidate cache for this floor
                _cacheService.InvalidateFloor(node.FloorId);
                
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
            var floorExists = await _context.Floors.AsNoTracking().AnyAsync(f => f.Id == node.FloorId);
            if (!floorExists)
            {
                return BadRequest($"Invalid floor_id {node.FloorId}: floor does not exist");
            }

            _context.RouteNodes.Add(node);
            await _context.SaveChangesAsync();

            // Invalidate cache for this floor
            _cacheService.InvalidateFloor(node.FloorId);

            return CreatedAtAction(nameof(GetRouteNode), new { id = node.Id }, node.ToGeoJsonFeature());
        }

        // DELETE: api/RouteNode/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRouteNode(int id)
        {
            var routeNode = await _context.RouteNodes.FindAsync(id);
            if (routeNode == null)
            {
                return NotFound();
            }

            var floorId = routeNode.FloorId;
            
            _context.RouteNodes.Remove(routeNode);
            await _context.SaveChangesAsync();

            // Invalidate cache for this floor
            _cacheService.InvalidateFloor(floorId);

            return NoContent();
        }

        // POST: api/RouteNode/findPath
        [HttpPost("findPath")]
        public async Task<ActionResult<FeatureCollection>> FindPath([FromBody] PathRequest request)
        {
            Console.WriteLine("=== FINDPATH ENDPOINT STARTED ===");
            try
            {
                Console.WriteLine($"[FINDPATH] Received request:");
                Console.WriteLine($"[FINDPATH] - User Location: {request?.UserLocation?.Latitude}, {request?.UserLocation?.Longitude}");
                Console.WriteLine($"[FINDPATH] - Destination POI ID: {request?.DestinationPoiId}");

                // Validate the request
                if (request.UserLocation == null || request.DestinationPoiId <= 0)
                {
                    Console.WriteLine("[FINDPATH] ERROR: Invalid request - User location or POI ID missing");
                    return BadRequest("User location and destination POI ID are required.");
                }

                Console.WriteLine("[FINDPATH] Step 1: Looking up destination POI...");
                // Find the destination POI
                var destinationPoi = await _context.Pois
                    .AsNoTracking()
                    .Include(p => p.ClosestNode)
                    .FirstOrDefaultAsync(p => p.Id == request.DestinationPoiId);

                if (destinationPoi == null)
                {
                    Console.WriteLine($"[FINDPATH] ERROR: POI with ID {request.DestinationPoiId} not found");
                    return NotFound($"POI with ID {request.DestinationPoiId} not found.");
                }

                Console.WriteLine($"[FINDPATH] Found POI: '{destinationPoi.Name}' on Floor ID: {destinationPoi.FloorId}");
                Console.WriteLine($"[FINDPATH] POI Closest Node ID: {destinationPoi.ClosestNodeId}");
                Console.WriteLine($"[FINDPATH] POI Closest Node Distance: {destinationPoi.ClosestNodeDistance}");

                if (destinationPoi.ClosestNodeId == null)
                {
                    Console.WriteLine($"[FINDPATH] ERROR: POI '{destinationPoi.Name}' does not have a closest node assigned");
                    return BadRequest($"POI '{destinationPoi.Name}' does not have a closest node assigned. Please ensure route nodes exist near this POI.");
                }

                Console.WriteLine("[FINDPATH] Step 2: Creating user point and finding closest node...");
                // Find the closest node to the user's current location
                var userPoint = new Point(request.UserLocation.Longitude, request.UserLocation.Latitude) { SRID = 4326 };
                Console.WriteLine($"[FINDPATH] User point created: X={userPoint.X}, Y={userPoint.Y}, SRID={userPoint.SRID}");
                
                var startNode = await _navigationService.FindClosestNodeAsync(userPoint, destinationPoi.FloorId);

                if (startNode == null)
                {
                    Console.WriteLine($"[FINDPATH] ERROR: No route nodes found on floor {destinationPoi.FloorId}");
                    return NotFound("No route nodes found on the specified floor.");
                }

                Console.WriteLine($"[FINDPATH] Found start node: ID={startNode.Id}, FloorId={startNode.FloorId}");
                Console.WriteLine($"[FINDPATH] Start node geometry: X={startNode.Geometry?.X}, Y={startNode.Geometry?.Y}");

                Console.WriteLine("[FINDPATH] Step 3: Finding shortest path...");
                Console.WriteLine($"[FINDPATH] Path from Start Node {startNode.Id} to End Node {destinationPoi.ClosestNodeId.Value}");
                
                // Find the shortest path
                var path = await _navigationService.FindShortestPathAsync(startNode.Id, destinationPoi.ClosestNodeId.Value);

                if (path == null || path.Count == 0)
                {
                    Console.WriteLine("[FINDPATH] ERROR: No path found between nodes");
                    return NotFound("No path found between the user location and destination POI.");
                }

                Console.WriteLine($"[FINDPATH] SUCCESS: Path found with {path.Count} nodes");
                for (int i = 0; i < path.Count; i++)
                {
                    var node = path[i];
                    Console.WriteLine($"[FINDPATH] Path[{i}]: Node ID={node.Id}, Geometry=({node.Geometry?.X}, {node.Geometry?.Y})");
                }

                Console.WriteLine("[FINDPATH] Step 4: Converting path to GeoJSON...");
                // Convert path to GeoJSON FeatureCollection
                var pathFeatures = new FeatureCollection();

                // Add path nodes as Point features
                Console.WriteLine("[FINDPATH] Adding path nodes as Point features...");
                foreach (var node in path)
                {
                    var feature = node.ToGeoJsonFeature();
                    feature.Attributes.Add("path_order", path.IndexOf(node));
                    feature.Attributes.Add("is_path_node", true);
                    pathFeatures.Add(feature);
                    Console.WriteLine($"[FINDPATH] Added node feature: ID={node.Id}, Order={path.IndexOf(node)}");
                }

                // Add path edges as LineString features
                Console.WriteLine("[FINDPATH] Adding path edges as LineString features...");
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var currentNode = path[i];
                    var nextNode = path[i + 1];

                    if (currentNode.Geometry != null && nextNode.Geometry != null)
                    {
                        var coordinates = new[]
                        {
                            new Coordinate(currentNode.Geometry.X, currentNode.Geometry.Y),
                            new Coordinate(nextNode.Geometry.X, nextNode.Geometry.Y)
                        };

                        var lineString = new LineString(coordinates) { SRID = 4326 };
                        var lineFeature = new Feature(lineString, new AttributesTable
                        {
                            { "path_segment", i },
                            { "is_path_edge", true },
                            { "from_node_id", currentNode.Id },
                            { "to_node_id", nextNode.Id }
                        });

                        pathFeatures.Add(lineFeature);
                        Console.WriteLine($"[FINDPATH] Added edge feature: Segment={i}, From={currentNode.Id}, To={nextNode.Id}");
                    }
                    else
                    {
                        Console.WriteLine($"[FINDPATH] WARNING: Skipped edge {i} - missing geometry on node {currentNode.Id} or {nextNode.Id}");
                    }
                }

                Console.WriteLine($"[FINDPATH] SUCCESS: Returning FeatureCollection with {pathFeatures.Count} features");
                Console.WriteLine("=== FINDPATH ENDPOINT COMPLETED ===");
                return Ok(pathFeatures);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FINDPATH] EXCEPTION: {ex.Message}");
                Console.WriteLine($"[FINDPATH] STACK TRACE: {ex.StackTrace}");
                Console.WriteLine("=== FINDPATH ENDPOINT FAILED ===");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/RouteNode/fixBidirectionalConnections
        [HttpPost("fixBidirectionalConnections")]
        public async Task<ActionResult<object>> FixBidirectionalConnections([FromBody] FixConnectionsRequest request)
        {
            Console.WriteLine("=== FIX BIDIRECTIONAL CONNECTIONS ENDPOINT STARTED ===");
            try
            {
                Console.WriteLine($"[FIX_CONNECTIONS] Received request for floor ID: {request.FloorId}");

                // Validate floor exists
                var floorExists = await _context.Floors.AsNoTracking().AnyAsync(f => f.Id == request.FloorId);
                if (!floorExists)
                {
                    Console.WriteLine($"[FIX_CONNECTIONS] ERROR: Floor {request.FloorId} does not exist");
                    return BadRequest($"Floor with ID {request.FloorId} does not exist.");
                }

                Console.WriteLine($"[FIX_CONNECTIONS] Floor {request.FloorId} exists, proceeding with bidirectional fix...");

                // Call the navigation service to fix bidirectional connections
                var (fixedConnections, report) = await _navigationService.FixBidirectionalConnectionsAsync(request.FloorId);
                
                // Invalidate cache for this floor
                _cacheService.InvalidateFloor(request.FloorId);

                Console.WriteLine($"[FIX_CONNECTIONS] Fix completed: {fixedConnections} connections fixed");
                Console.WriteLine("=== FIX BIDIRECTIONAL CONNECTIONS ENDPOINT COMPLETED ===");

                return Ok(new
                {
                    success = true,
                    floorId = request.FloorId,
                    fixedConnections = fixedConnections,
                    report = report,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FIX_CONNECTIONS] EXCEPTION: {ex.Message}");
                Console.WriteLine($"[FIX_CONNECTIONS] STACK TRACE: {ex.StackTrace}");
                Console.WriteLine("=== FIX BIDIRECTIONAL CONNECTIONS ENDPOINT FAILED ===");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Internal server error: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // POST: api/RouteNode/addConnection
        [HttpPost("addConnection")]
        public async Task<IActionResult> AddConnection([FromBody] AddConnectionRequest request)
        {
            try
            {
                // 1. Get both nodes from database
                var node1 = await _context.RouteNodes.FindAsync(request.NodeId1);
                var node2 = await _context.RouteNodes.FindAsync(request.NodeId2);
                
                if (node1 == null || node2 == null)
                {
                    return NotFound("One or both nodes not found");
                }

                // 2. Get current connections (handle null)
                var connections1 = node1.ConnectedNodeIds ?? new List<int>();
                var connections2 = node2.ConnectedNodeIds ?? new List<int>();

                // 3. Add bidirectional connections (avoid duplicates)
                if (!connections1.Contains(request.NodeId2))
                {
                    connections1.Add(request.NodeId2);
                    node1.ConnectedNodeIds = connections1;
                }
                
                if (!connections2.Contains(request.NodeId1))
                {
                    connections2.Add(request.NodeId1);
                    node2.ConnectedNodeIds = connections2;
                }

                // 4. Save changes
                await _context.SaveChangesAsync();
                
                // Invalidate caches
                _cacheService.InvalidateFloor(node1.FloorId);
                if (node2.FloorId != node1.FloorId)
                {
                    _cacheService.InvalidateFloor(node2.FloorId);
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding connection: {ex.Message}");
            }
        }

        // POST: api/RouteNode/navigateToLevel
        [HttpPost("navigateToLevel")]
        public async Task<ActionResult<FeatureCollection>> NavigateToLevel([FromBody] LevelNavigationRequest request)
        {
            Console.WriteLine("=== NAVIGATE TO LEVEL ENDPOINT STARTED ===");
            try
            {
                Console.WriteLine($"[NAV_TO_LEVEL] Received request:");
                Console.WriteLine($"[NAV_TO_LEVEL] - Current Node ID: {request?.CurrentNodeId}");
                Console.WriteLine($"[NAV_TO_LEVEL] - Target Level: {request?.TargetLevel}");
                Console.WriteLine($"[NAV_TO_LEVEL] - Floor ID: {request?.FloorId}");

                // Validate the request
                if (request.CurrentNodeId <= 0 || request.TargetLevel <= 0)
                {
                    Console.WriteLine("[NAV_TO_LEVEL] ERROR: Invalid request - missing required fields");
                    return BadRequest("Current node ID and target level are required.");
                }

                Console.WriteLine("[NAV_TO_LEVEL] Step 1: Loading start node from CACHE...");
                
                // Get the start node from CACHE instead of database (MUCH FASTER!)
                var startNode = await _cacheService.GetNodeByIdAsync(request.CurrentNodeId);

                if (startNode == null)
                {
                    Console.WriteLine($"[NAV_TO_LEVEL] ERROR: Node {request.CurrentNodeId} not found");
                    return NotFound($"Node with ID {request.CurrentNodeId} not found.");
                }

                Console.WriteLine($"[NAV_TO_LEVEL] Start node loaded: ID={startNode.Id}, Level={startNode.Level ?? -1}, FloorId={startNode.FloorId}");

                // Use the node's floor ID if not explicitly provided
                int floorId = request.FloorId ?? startNode.FloorId;
                
                if (!request.FloorId.HasValue)
                {
                    Console.WriteLine($"[NAV_TO_LEVEL] FloorId not provided, using node's floor: {floorId}");
                }

                // Check if we're already at a node on the target level
                if (startNode.Level.HasValue && startNode.Level.Value == request.TargetLevel)
                {
                    Console.WriteLine($"[NAV_TO_LEVEL] User is already at target level {request.TargetLevel}");
                    // Return a single-node path (the current position)
                    var singleNodePath = new FeatureCollection();
                    var feature = startNode.ToGeoJsonFeature();
                    feature.Attributes.Add("path_order", 0);
                    feature.Attributes.Add("is_path_node", true);
                    feature.Attributes.Add("node_level", startNode.Level.Value);
                    singleNodePath.Add(feature);
                    return Ok(singleNodePath);
                }

                Console.WriteLine("[NAV_TO_LEVEL] Step 2: Finding cross-level path...");
                
                // Find the path to the target level
                var path = await _navigationService.FindCrossLevelPathAsync(startNode.Id, request.TargetLevel, floorId);

                if (path == null || path.Count == 0)
                {
                    Console.WriteLine("[NAV_TO_LEVEL] ERROR: No path found to target level");
                    return NotFound($"No path found from node {request.CurrentNodeId} to level {request.TargetLevel}.");
                }

                Console.WriteLine($"[NAV_TO_LEVEL] SUCCESS: Path found with {path.Count} nodes");
                for (int i = 0; i < path.Count; i++)
                {
                    var node = path[i];
                    Console.WriteLine($"[NAV_TO_LEVEL] Path[{i}]: Node ID={node.Id}, Level={node.Level}, Geometry=({node.Geometry?.X}, {node.Geometry?.Y})");
                }

                Console.WriteLine("[NAV_TO_LEVEL] Step 3: Converting path to GeoJSON...");
                
                // Convert path to GeoJSON FeatureCollection
                var pathFeatures = new FeatureCollection();

                // Add path nodes as Point features
                Console.WriteLine("[NAV_TO_LEVEL] Adding path nodes as Point features...");
                foreach (var node in path)
                {
                    var feature = node.ToGeoJsonFeature();
                    feature.Attributes.Add("path_order", path.IndexOf(node));
                    feature.Attributes.Add("is_path_node", true);
                    feature.Attributes.Add("node_level", node.Level ?? -1);
                    
                    // Mark level transitions
                    if (path.IndexOf(node) > 0)
                    {
                        var prevNode = path[path.IndexOf(node) - 1];
                        if (prevNode.Level != node.Level)
                        {
                            feature.Attributes.Add("is_level_transition", true);
                            feature.Attributes.Add("transition_from_level", prevNode.Level ?? -1);
                            feature.Attributes.Add("transition_to_level", node.Level ?? -1);
                            Console.WriteLine($"[NAV_TO_LEVEL] Level transition detected: {prevNode.Level} -> {node.Level} at node {node.Id}");
                        }
                    }
                    
                    pathFeatures.Add(feature);
                    Console.WriteLine($"[NAV_TO_LEVEL] Added node feature: ID={node.Id}, Level={node.Level}, Order={path.IndexOf(node)}");
                }

                // Add path edges as LineString features
                Console.WriteLine("[NAV_TO_LEVEL] Adding path edges as LineString features...");
                for (int i = 0; i < path.Count - 1; i++)
                {
                    var currentNode = path[i];
                    var nextNode = path[i + 1];

                    if (currentNode.Geometry != null && nextNode.Geometry != null)
                    {
                        var coordinates = new[]
                        {
                            new Coordinate(currentNode.Geometry.X, currentNode.Geometry.Y),
                            new Coordinate(nextNode.Geometry.X, nextNode.Geometry.Y)
                        };

                        var lineString = new LineString(coordinates) { SRID = 4326 };
                        var lineFeature = new Feature(lineString, new AttributesTable
                        {
                            { "path_segment", i },
                            { "is_path_edge", true },
                            { "from_node_id", currentNode.Id },
                            { "to_node_id", nextNode.Id },
                            { "from_level", currentNode.Level ?? -1 },
                            { "to_level", nextNode.Level ?? -1 },
                            { "is_level_transition", currentNode.Level != nextNode.Level }
                        });

                        pathFeatures.Add(lineFeature);
                        Console.WriteLine($"[NAV_TO_LEVEL] Added edge feature: Segment={i}, From={currentNode.Id} (L{currentNode.Level}), To={nextNode.Id} (L{nextNode.Level})");
                    }
                    else
                    {
                        Console.WriteLine($"[NAV_TO_LEVEL] WARNING: Skipped edge {i} - missing geometry on node {currentNode.Id} or {nextNode.Id}");
                    }
                }

                Console.WriteLine($"[NAV_TO_LEVEL] SUCCESS: Returning FeatureCollection with {pathFeatures.Count} features");
                Console.WriteLine("=== NAVIGATE TO LEVEL ENDPOINT COMPLETED ===");
                return Ok(pathFeatures);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NAV_TO_LEVEL] EXCEPTION: {ex.Message}");
                Console.WriteLine($"[NAV_TO_LEVEL] STACK TRACE: {ex.StackTrace}");
                Console.WriteLine("=== NAVIGATE TO LEVEL ENDPOINT FAILED ===");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/RouteNode/detectConnectionPoints
        [HttpPost("detectConnectionPoints")]
        public async Task<ActionResult<object>> DetectConnectionPoints([FromQuery] int? floorId = null)
        {
            Console.WriteLine("=== DETECT CONNECTION POINTS ENDPOINT STARTED ===");
            try
            {
                Console.WriteLine($"[DETECT_CONN] Starting detection for {(floorId.HasValue ? $"floor {floorId}" : "all floors")}");

                var (detected, report) = await _connectionDetectionService.DetectAndMarkConnectionPointsAsync(floorId);

                Console.WriteLine($"[DETECT_CONN] Detection completed: {detected} connection points detected");
                Console.WriteLine("=== DETECT CONNECTION POINTS ENDPOINT COMPLETED ===");

                return Ok(new
                {
                    success = true,
                    detectedCount = detected,
                    floorId = floorId,
                    report = report,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DETECT_CONN] EXCEPTION: {ex.Message}");
                Console.WriteLine($"[DETECT_CONN] STACK TRACE: {ex.StackTrace}");
                Console.WriteLine("=== DETECT CONNECTION POINTS ENDPOINT FAILED ===");
                return StatusCode(500, new
                {
                    success = false,
                    error = $"Internal server error: {ex.Message}",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // GET: api/RouteNode/connectionPoints
        [HttpGet("connectionPoints")]
        public async Task<ActionResult<FeatureCollection>> GetConnectionPoints()
        {
            var connectionPoints = await _cacheService.GetConnectionPointsAsync();
            
            var features = new FeatureCollection();
            foreach (var node in connectionPoints)
            {
                var feature = node.ToGeoJsonFeature();
                feature.Attributes.Add("is_connection_point", true);
                feature.Attributes.Add("connection_type", node.ConnectionType);
                feature.Attributes.Add("connection_priority", node.ConnectionPriority);
                feature.Attributes.Add("connected_levels", node.ConnectedLevels);
                features.Add(feature);
            }

            return Ok(features);
        }

        // POST: api/RouteNode/invalidateCache
        [HttpPost("invalidateCache")]
        public ActionResult<object> InvalidateCache([FromQuery] int? floorId = null)
        {
            if (floorId.HasValue)
            {
                _cacheService.InvalidateFloor(floorId.Value);
                return Ok(new
                {
                    success = true,
                    message = $"Cache invalidated for floor {floorId}",
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _cacheService.InvalidateAll();
                return Ok(new
                {
                    success = true,
                    message = "All caches invalidated",
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // GET: api/RouteNode/cacheStatistics
        [HttpGet("cacheStatistics")]
        public ActionResult<object> GetCacheStatistics()
        {
            var (hits, misses, hitRate) = _cacheService.GetStatistics();
            
            return Ok(new
            {
                cacheHits = hits,
                cacheMisses = misses,
                totalRequests = hits + misses,
                hitRate = $"{hitRate:F2}%",
                timestamp = DateTime.UtcNow
            });
        }

        private bool RouteNodeExists(int id)
        {
            return _context.RouteNodes.Any(e => e.Id == id);
        }
    }
}