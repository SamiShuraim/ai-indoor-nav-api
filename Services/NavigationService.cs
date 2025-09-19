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