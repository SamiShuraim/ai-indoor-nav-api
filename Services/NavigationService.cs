using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    public class NavigationService
    {
        private readonly MyDbContext _context;

        public NavigationService(MyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Fixes unidirectional connections to make them bidirectional for all nodes on a specific floor
        /// </summary>
        public async Task<(int fixedConnections, string report)> FixBidirectionalConnectionsAsync(int floorId)
        {
            Console.WriteLine($"[NAV_SERVICE] Starting bidirectional connection fix for floor {floorId}");
            
            var nodesOnFloor = await _context.RouteNodes
                .Where(n => n.FloorId == floorId && n.IsVisible)
                .ToListAsync();

            Console.WriteLine($"[NAV_SERVICE] Found {nodesOnFloor.Count} visible nodes on floor {floorId}");

            int fixedConnections = 0;
            var reportLines = new List<string>();
            var modificationsNeeded = new Dictionary<int, HashSet<int>>();

            // First pass: identify missing bidirectional connections
            foreach (var node in nodesOnFloor)
            {
                Console.WriteLine($"[NAV_SERVICE] Analyzing node {node.Id} with connections: [{string.Join(", ", node.ConnectedNodeIds)}]");
                
                foreach (var connectedNodeId in node.ConnectedNodeIds)
                {
                    var connectedNode = nodesOnFloor.FirstOrDefault(n => n.Id == connectedNodeId);
                    if (connectedNode == null)
                    {
                        Console.WriteLine($"[NAV_SERVICE] WARNING: Node {node.Id} connects to {connectedNodeId} but that node is not visible on this floor");
                        reportLines.Add($"WARNING: Node {node.Id} connects to {connectedNodeId} but that node is not visible on floor {floorId}");
                        continue;
                    }

                    // Check if the connection is bidirectional
                    if (!connectedNode.ConnectedNodeIds.Contains(node.Id))
                    {
                        Console.WriteLine($"[NAV_SERVICE] MISSING: Node {connectedNodeId} should connect back to {node.Id}");
                        
                        if (!modificationsNeeded.ContainsKey(connectedNodeId))
                        {
                            modificationsNeeded[connectedNodeId] = new HashSet<int>(connectedNode.ConnectedNodeIds);
                        }
                        modificationsNeeded[connectedNodeId].Add(node.Id);
                        fixedConnections++;
                    }
                }
            }

            Console.WriteLine($"[NAV_SERVICE] Found {fixedConnections} missing bidirectional connections");
            reportLines.Add($"Found {fixedConnections} missing bidirectional connections on floor {floorId}");

            // Second pass: apply the fixes
            foreach (var modification in modificationsNeeded)
            {
                var nodeId = modification.Key;
                var newConnections = modification.Value.ToList();
                
                var nodeToUpdate = nodesOnFloor.First(n => n.Id == nodeId);
                var oldConnections = string.Join(", ", nodeToUpdate.ConnectedNodeIds);
                
                nodeToUpdate.ConnectedNodeIds = newConnections;
                nodeToUpdate.UpdatedAt = DateTime.UtcNow;
                
                var newConnectionsStr = string.Join(", ", newConnections);
                Console.WriteLine($"[NAV_SERVICE] Updated node {nodeId} connections: [{oldConnections}] → [{newConnectionsStr}]");
                reportLines.Add($"Updated node {nodeId}: [{oldConnections}] → [{newConnectionsStr}]");
            }

            if (fixedConnections > 0)
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[NAV_SERVICE] Saved {fixedConnections} connection fixes to database");
                reportLines.Add($"Successfully saved {fixedConnections} connection fixes to database");
            }
            else
            {
                reportLines.Add("No bidirectional connection fixes needed - all connections are already bidirectional");
            }

            var report = string.Join("\n", reportLines);
            Console.WriteLine($"[NAV_SERVICE] Bidirectional fix completed. Report:\n{report}");
            
            return (fixedConnections, report);
        }

        /// <summary>
        /// Updates the closest node for POIs when a new node is created
        /// </summary>
        public async Task UpdatePoiClosestNodesAsync(RouteNode newNode)
        {
            // Get all POIs on the same floor
            var poisOnFloor = await _context.Pois
                .Where(p => p.FloorId == newNode.FloorId)
                .ToListAsync();

            foreach (var poi in poisOnFloor)
            {
                if (poi.Geometry == null || newNode.Geometry == null) continue;

                // Calculate distance from POI center to the new node
                var poiCenter = poi.Geometry.Centroid;
                var distance = CalculateDistance(poiCenter, newNode.Geometry);

                // Update if this is the first node or if it's closer than the current closest
                if (poi.ClosestNodeId == null || distance < poi.ClosestNodeDistance)
                {
                    poi.ClosestNodeId = newNode.Id;
                    poi.ClosestNodeDistance = distance;
                    poi.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Recalculates the closest node for all POIs on a specific floor or all floors
        /// This is a separate operation that can be called manually when needed
        /// </summary>
        public async Task<(int updatedPois, string report)> RecalculateAllPoiClosestNodesAsync(int? floorId = null)
        {
            Console.WriteLine($"[NAV_SERVICE] Starting POI closest nodes recalculation for {(floorId.HasValue ? $"floor {floorId}" : "all floors")}");
            
            // Get POIs to process
            var poisQuery = _context.Pois.AsQueryable();
            if (floorId.HasValue)
            {
                poisQuery = poisQuery.Where(p => p.FloorId == floorId.Value);
            }
            var poisToProcess = await poisQuery.ToListAsync();
            
            Console.WriteLine($"[NAV_SERVICE] Found {poisToProcess.Count} POIs to process");
            
            var reportLines = new List<string>();
            int updatedPois = 0;
            
            // Group POIs by floor for efficient processing
            var poisByFloor = poisToProcess.GroupBy(p => p.FloorId).ToList();
            
            foreach (var floorGroup in poisByFloor)
            {
                var currentFloorId = floorGroup.Key;
                var poisOnFloor = floorGroup.ToList();
                
                Console.WriteLine($"[NAV_SERVICE] Processing {poisOnFloor.Count} POIs on floor {currentFloorId}");
                reportLines.Add($"Processing {poisOnFloor.Count} POIs on floor {currentFloorId}");
                
                // Get all visible nodes on this floor
                var nodesOnFloor = await _context.RouteNodes
                    .Where(n => n.FloorId == currentFloorId && n.IsVisible)
                    .ToListAsync();
                
                Console.WriteLine($"[NAV_SERVICE] Found {nodesOnFloor.Count} visible nodes on floor {currentFloorId}");
                
                if (nodesOnFloor.Count == 0)
                {
                    reportLines.Add($"WARNING: No visible nodes found on floor {currentFloorId}, skipping POIs on this floor");
                    continue;
                }
                
                // Process each POI on this floor
                foreach (var poi in poisOnFloor)
                {
                    if (poi.Geometry == null)
                    {
                        Console.WriteLine($"[NAV_SERVICE] Skipping POI {poi.Id} ({poi.Name}) - null geometry");
                        continue;
                    }
                    
                    var poiCenter = poi.Geometry.Centroid;
                    RouteNode? closestNode = null;
                    double minDistance = double.MaxValue;
                    
                    // Find the closest node to this POI
                    foreach (var node in nodesOnFloor)
                    {
                        if (node.Geometry == null) continue;
                        
                        var distance = CalculateDistance(poiCenter, node.Geometry);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestNode = node;
                        }
                    }
                    
                    // Update the POI if we found a closest node
                    if (closestNode != null)
                    {
                        var oldNodeId = poi.ClosestNodeId;
                        var oldDistance = poi.ClosestNodeDistance;
                        
                        poi.ClosestNodeId = closestNode.Id;
                        poi.ClosestNodeDistance = minDistance;
                        poi.UpdatedAt = DateTime.UtcNow;
                        updatedPois++;
                        
                        Console.WriteLine($"[NAV_SERVICE] Updated POI {poi.Id} ({poi.Name}): Node {oldNodeId} (dist: {oldDistance:F6}) → Node {closestNode.Id} (dist: {minDistance:F6})");
                        reportLines.Add($"POI '{poi.Name}' (ID: {poi.Id}): Node {oldNodeId} → Node {closestNode.Id} (distance: {minDistance:F6})");
                    }
                    else
                    {
                        Console.WriteLine($"[NAV_SERVICE] No closest node found for POI {poi.Id} ({poi.Name})");
                        reportLines.Add($"WARNING: No closest node found for POI '{poi.Name}' (ID: {poi.Id})");
                    }
                }
            }
            
            // Save all changes
            if (updatedPois > 0)
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[NAV_SERVICE] Saved {updatedPois} POI updates to database");
                reportLines.Add($"Successfully updated {updatedPois} POIs and saved to database");
            }
            else
            {
                reportLines.Add("No POIs were updated");
            }
            
            var report = string.Join("\n", reportLines);
            Console.WriteLine($"[NAV_SERVICE] POI closest nodes recalculation completed. Updated {updatedPois} POIs");
            
            return (updatedPois, report);
        }

        /// <summary>
        /// Calculates the Euclidean distance between two points
        /// </summary>
        public static double CalculateDistance(Point point1, Point point2)
        {
            var dx = point1.X - point2.X;
            var dy = point1.Y - point2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Finds the closest node to a given point on a specific floor
        /// </summary>
        public async Task<RouteNode?> FindClosestNodeAsync(Point location, int floorId)
        {
            Console.WriteLine($"[NAV_SERVICE] FindClosestNodeAsync called");
            Console.WriteLine($"[NAV_SERVICE] - Location: X={location.X}, Y={location.Y}, FloorId={floorId}");
            
            var nodesOnFloor = await _context.RouteNodes
                .Where(n => n.FloorId == floorId && n.IsVisible)
                .ToListAsync();

            Console.WriteLine($"[NAV_SERVICE] Found {nodesOnFloor.Count} visible nodes on floor {floorId}");

            RouteNode? closestNode = null;
            double minDistance = double.MaxValue;

            foreach (var node in nodesOnFloor)
            {
                if (node.Geometry == null)
                {
                    Console.WriteLine($"[NAV_SERVICE] Skipping node {node.Id} - null geometry");
                    continue;
                }

                var distance = CalculateDistance(location, node.Geometry);
                Console.WriteLine($"[NAV_SERVICE] Node {node.Id} distance: {distance:F6}");
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = node;
                    Console.WriteLine($"[NAV_SERVICE] New closest node: {node.Id} with distance {distance:F6}");
                }
            }

            if (closestNode != null)
            {
                Console.WriteLine($"[NAV_SERVICE] Final closest node: ID={closestNode.Id}, Distance={minDistance:F6}");
            }
            else
            {
                Console.WriteLine($"[NAV_SERVICE] No closest node found");
            }

            return closestNode;
        }

        /// <summary>
        /// Implements Dijkstra's algorithm to find the shortest path between two nodes
        /// </summary>
        public async Task<List<RouteNode>?> FindShortestPathAsync(int startNodeId, int endNodeId)
        {
            Console.WriteLine($"[NAV_SERVICE] FindShortestPathAsync called");
            Console.WriteLine($"[NAV_SERVICE] - StartNodeId: {startNodeId}, EndNodeId: {endNodeId}");
            
            // Get all nodes on the same floor (assuming both nodes are on the same floor)
            var startNode = await _context.RouteNodes.FindAsync(startNodeId);
            var endNode = await _context.RouteNodes.FindAsync(endNodeId);
            
            Console.WriteLine($"[NAV_SERVICE] Start node: {(startNode != null ? $"ID={startNode.Id}, FloorId={startNode.FloorId}" : "NULL")}");
            Console.WriteLine($"[NAV_SERVICE] End node: {(endNode != null ? $"ID={endNode.Id}, FloorId={endNode.FloorId}" : "NULL")}");
            
            if (startNode == null || endNode == null || startNode.FloorId != endNode.FloorId)
            {
                Console.WriteLine($"[NAV_SERVICE] ERROR: Invalid nodes or floor mismatch");
                Console.WriteLine($"[NAV_SERVICE] - Start node null: {startNode == null}");
                Console.WriteLine($"[NAV_SERVICE] - End node null: {endNode == null}");
                if (startNode != null && endNode != null)
                {
                    Console.WriteLine($"[NAV_SERVICE] - Floor mismatch: Start={startNode.FloorId}, End={endNode.FloorId}");
                }
                return null;
            }

            var allNodes = await _context.RouteNodes
                .Where(n => n.FloorId == startNode.FloorId && n.IsVisible)
                .ToListAsync();

            Console.WriteLine($"[NAV_SERVICE] Retrieved {allNodes.Count} visible nodes on floor {startNode.FloorId}");
            Console.WriteLine($"[NAV_SERVICE] Calling DijkstraShortestPath...");

            var result = DijkstraShortestPath(allNodes, startNodeId, endNodeId);
            
            Console.WriteLine($"[NAV_SERVICE] DijkstraShortestPath returned: {(result != null ? $"{result.Count} nodes" : "NULL")}");
            
            return result;
        }

        private List<RouteNode>? DijkstraShortestPath(List<RouteNode> nodes, int startId, int endId)
        {
            Console.WriteLine($"[DIJKSTRA] Starting Dijkstra algorithm");
            Console.WriteLine($"[DIJKSTRA] - Nodes count: {nodes.Count}, StartId: {startId}, EndId: {endId}");
            
            var nodeDict = nodes.ToDictionary(n => n.Id, n => n);
            var distances = new Dictionary<int, double>();
            var previous = new Dictionary<int, int?>();
            var unvisited = new HashSet<int>();

            Console.WriteLine($"[DIJKSTRA] Node dictionary created with {nodeDict.Count} entries");

            // Initialize distances
            foreach (var node in nodes)
            {
                distances[node.Id] = node.Id == startId ? 0 : double.MaxValue;
                previous[node.Id] = null;
                unvisited.Add(node.Id);
                Console.WriteLine($"[DIJKSTRA] Initialized node {node.Id} - Distance: {(node.Id == startId ? "0" : "∞")}, Connected to: [{string.Join(", ", node.ConnectedNodeIds)}]");
            }

            Console.WriteLine($"[DIJKSTRA] Starting main algorithm loop with {unvisited.Count} unvisited nodes");
            int iteration = 0;

            while (unvisited.Count > 0)
            {
                iteration++;
                Console.WriteLine($"[DIJKSTRA] --- Iteration {iteration} ---");
                
                // Find unvisited node with minimum distance
                var currentId = unvisited.OrderBy(id => distances[id]).First();
                var currentDistance = distances[currentId];
                unvisited.Remove(currentId);

                Console.WriteLine($"[DIJKSTRA] Current node: {currentId} with distance {currentDistance:F6}");
                Console.WriteLine($"[DIJKSTRA] Remaining unvisited: {unvisited.Count}");

                if (currentId == endId) 
                {
                    Console.WriteLine($"[DIJKSTRA] Reached end node {endId}! Breaking.");
                    break;
                }

                var currentNode = nodeDict[currentId];
                Console.WriteLine($"[DIJKSTRA] Processing connections for node {currentId}");
                Console.WriteLine($"[DIJKSTRA] Node has {currentNode.ConnectedNodeIds.Count} connections: [{string.Join(", ", currentNode.ConnectedNodeIds)}]");
                
                // Check all connected nodes
                foreach (var connectedId in currentNode.ConnectedNodeIds)
                {
                    Console.WriteLine($"[DIJKSTRA] Checking connection to node {connectedId}");
                    
                    if (!unvisited.Contains(connectedId))
                    {
                        Console.WriteLine($"[DIJKSTRA] - Node {connectedId} already visited, skipping");
                        continue;
                    }
                    
                    if (!nodeDict.ContainsKey(connectedId))
                    {
                        Console.WriteLine($"[DIJKSTRA] - Node {connectedId} not in node dictionary, skipping");
                        continue;
                    }

                    var connectedNode = nodeDict[connectedId];
                    if (currentNode.Geometry == null || connectedNode.Geometry == null)
                    {
                        Console.WriteLine($"[DIJKSTRA] - Missing geometry on node {currentId} or {connectedId}, skipping");
                        continue;
                    }

                    var edgeWeight = CalculateDistance(currentNode.Geometry, connectedNode.Geometry);
                    var altDistance = distances[currentId] + edgeWeight;
                    var currentConnectedDistance = distances[connectedId];

                    Console.WriteLine($"[DIJKSTRA] - Edge weight to {connectedId}: {edgeWeight:F6}");
                    Console.WriteLine($"[DIJKSTRA] - Alternative distance: {altDistance:F6}");
                    Console.WriteLine($"[DIJKSTRA] - Current distance to {connectedId}: {(currentConnectedDistance == double.MaxValue ? "∞" : currentConnectedDistance.ToString("F6"))}");

                    if (altDistance < distances[connectedId])
                    {
                        distances[connectedId] = altDistance;
                        previous[connectedId] = currentId;
                        Console.WriteLine($"[DIJKSTRA] - Updated distance to {connectedId}: {altDistance:F6} via {currentId}");
                    }
                    else
                    {
                        Console.WriteLine($"[DIJKSTRA] - No improvement for {connectedId}");
                    }
                }
            }

            Console.WriteLine($"[DIJKSTRA] Algorithm completed after {iteration} iterations");
            Console.WriteLine($"[DIJKSTRA] Final distance to end node {endId}: {(distances[endId] == double.MaxValue ? "∞" : distances[endId].ToString("F6"))}");

            // Reconstruct path
            if (!previous[endId].HasValue && startId != endId)
            {
                Console.WriteLine($"[DIJKSTRA] No path found - end node {endId} has no previous node");
                return null;
            }

            Console.WriteLine($"[DIJKSTRA] Reconstructing path from {endId} to {startId}");
            var path = new List<RouteNode>();
            int? current = endId;
            
            while (current.HasValue)
            {
                var node = nodeDict[current.Value];
                path.Insert(0, node);
                Console.WriteLine($"[DIJKSTRA] Path step: {current.Value}");
                current = previous[current.Value];
            }

            Console.WriteLine($"[DIJKSTRA] Final path has {path.Count} nodes");
            return path;
        }
    }
}