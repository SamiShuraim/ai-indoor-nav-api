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
            var nodesOnFloor = await _context.RouteNodes
                .Where(n => n.FloorId == floorId && n.IsVisible)
                .ToListAsync();

            RouteNode? closestNode = null;
            double minDistance = double.MaxValue;

            foreach (var node in nodesOnFloor)
            {
                if (node.Geometry == null) continue;

                var distance = CalculateDistance(location, node.Geometry);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = node;
                }
            }

            return closestNode;
        }

        /// <summary>
        /// Implements Dijkstra's algorithm to find the shortest path between two nodes
        /// </summary>
        public async Task<List<RouteNode>?> FindShortestPathAsync(int startNodeId, int endNodeId)
        {
            // Get all nodes on the same floor (assuming both nodes are on the same floor)
            var startNode = await _context.RouteNodes.FindAsync(startNodeId);
            var endNode = await _context.RouteNodes.FindAsync(endNodeId);
            
            if (startNode == null || endNode == null || startNode.FloorId != endNode.FloorId)
                return null;

            var allNodes = await _context.RouteNodes
                .Where(n => n.FloorId == startNode.FloorId && n.IsVisible)
                .ToListAsync();

            return DijkstraShortestPath(allNodes, startNodeId, endNodeId);
        }

        private List<RouteNode>? DijkstraShortestPath(List<RouteNode> nodes, int startId, int endId)
        {
            var nodeDict = nodes.ToDictionary(n => n.Id, n => n);
            var distances = new Dictionary<int, double>();
            var previous = new Dictionary<int, int?>();
            var unvisited = new HashSet<int>();

            // Initialize distances
            foreach (var node in nodes)
            {
                distances[node.Id] = node.Id == startId ? 0 : double.MaxValue;
                previous[node.Id] = null;
                unvisited.Add(node.Id);
            }

            while (unvisited.Count > 0)
            {
                // Find unvisited node with minimum distance
                var currentId = unvisited.OrderBy(id => distances[id]).First();
                unvisited.Remove(currentId);

                if (currentId == endId) break;

                var currentNode = nodeDict[currentId];
                
                // Check all connected nodes
                foreach (var connectedId in currentNode.ConnectedNodeIds)
                {
                    if (!unvisited.Contains(connectedId) || !nodeDict.ContainsKey(connectedId)) 
                        continue;

                    var connectedNode = nodeDict[connectedId];
                    if (currentNode.Geometry == null || connectedNode.Geometry == null) 
                        continue;

                    var edgeWeight = CalculateDistance(currentNode.Geometry, connectedNode.Geometry);
                    var altDistance = distances[currentId] + edgeWeight;

                    if (altDistance < distances[connectedId])
                    {
                        distances[connectedId] = altDistance;
                        previous[connectedId] = currentId;
                    }
                }
            }

            // Reconstruct path
            if (!previous[endId].HasValue && startId != endId)
                return null; // No path found

            var path = new List<RouteNode>();
            int? current = endId;
            
            while (current.HasValue)
            {
                path.Insert(0, nodeDict[current.Value]);
                current = previous[current.Value];
            }

            return path;
        }
    }
}