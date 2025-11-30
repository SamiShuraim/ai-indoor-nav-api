using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    public class ConnectionPointDetectionService
    {
        private readonly MyDbContext _context;
        private readonly ILogger<ConnectionPointDetectionService> _logger;
        private readonly NodeCacheService _cacheService;

        public ConnectionPointDetectionService(
            MyDbContext context,
            ILogger<ConnectionPointDetectionService> logger,
            NodeCacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Automatically detects and marks nodes as connection points based on their connections
        /// A node is a connection point if it connects to nodes on different levels
        /// </summary>
        public async Task<(int detected, string report)> DetectAndMarkConnectionPointsAsync(int? floorId = null)
        {
            _logger.LogInformation($"[CONNECTION_DETECT] Starting connection point detection for {(floorId.HasValue ? $"floor {floorId}" : "all floors")}");

            // Get all visible nodes
            var query = _context.RouteNodes.Where(n => n.IsVisible);
            if (floorId.HasValue)
            {
                query = query.Where(n => n.FloorId == floorId.Value);
            }

            var nodes = await query.ToListAsync();
            _logger.LogInformation($"[CONNECTION_DETECT] Analyzing {nodes.Count} nodes");

            var reportLines = new List<string>();
            int detectedCount = 0;
            int updatedCount = 0;

            // Create a lookup dictionary for fast node access
            var allNodesDict = await _context.RouteNodes
                .AsNoTracking()
                .Where(n => n.IsVisible)
                .ToDictionaryAsync(n => n.Id);

            foreach (var node in nodes)
            {
                if (!node.Level.HasValue)
                {
                    _logger.LogDebug($"[CONNECTION_DETECT] Node {node.Id} has no level, skipping");
                    continue;
                }

                var currentLevel = node.Level.Value;
                var connectedLevels = new HashSet<int>();
                var hasLevelTransition = false;

                // Check all connected nodes
                foreach (var connectedId in node.ConnectedNodeIds)
                {
                    if (!allNodesDict.ContainsKey(connectedId))
                    {
                        _logger.LogDebug($"[CONNECTION_DETECT] Node {node.Id} connects to missing node {connectedId}");
                        continue;
                    }

                    var connectedNode = allNodesDict[connectedId];
                    
                    if (!connectedNode.Level.HasValue)
                    {
                        _logger.LogDebug($"[CONNECTION_DETECT] Connected node {connectedId} has no level");
                        continue;
                    }

                    var connectedLevel = connectedNode.Level.Value;
                    connectedLevels.Add(connectedLevel);

                    // Check if this connection crosses levels
                    if (connectedLevel != currentLevel)
                    {
                        hasLevelTransition = true;
                        _logger.LogDebug($"[CONNECTION_DETECT] Node {node.Id} (L{currentLevel}) connects to node {connectedId} (L{connectedLevel}) - LEVEL TRANSITION");
                    }
                }

                // If node connects to different levels, mark it as a connection point
                if (hasLevelTransition)
                {
                    var wasAlreadyMarked = node.IsConnectionPoint;
                    
                    node.IsConnectionPoint = true;
                    node.ConnectedLevels = connectedLevels.ToList();
                    node.UpdatedAt = DateTime.UtcNow;

                    // Auto-detect connection type based on the connections
                    if (string.IsNullOrEmpty(node.ConnectionType))
                    {
                        node.ConnectionType = DetectConnectionType(node, connectedLevels);
                        node.ConnectionPriority = GetConnectionPriority(node.ConnectionType);
                    }

                    if (!wasAlreadyMarked)
                    {
                        detectedCount++;
                        _logger.LogInformation($"[CONNECTION_DETECT] DETECTED: Node {node.Id} connects levels {string.Join(", ", connectedLevels.OrderBy(l => l))} - Type: {node.ConnectionType}");
                        reportLines.Add($"Node {node.Id}: Connection point detected connecting levels {string.Join(", ", connectedLevels.OrderBy(l => l))} (Type: {node.ConnectionType ?? "unknown"})");
                    }
                    else
                    {
                        updatedCount++;
                        _logger.LogDebug($"[CONNECTION_DETECT] UPDATED: Node {node.Id} already marked, updating connected levels");
                    }
                }
                else if (node.IsConnectionPoint && !hasLevelTransition)
                {
                    // Node was marked but no longer connects different levels
                    _logger.LogWarning($"[CONNECTION_DETECT] Node {node.Id} was marked as connection point but no longer connects different levels - unmarking");
                    node.IsConnectionPoint = false;
                    node.ConnectionType = null;
                    node.ConnectedLevels = new List<int>();
                    node.ConnectionPriority = null;
                    node.UpdatedAt = DateTime.UtcNow;
                    reportLines.Add($"Node {node.Id}: Unmarked (no longer connects different levels)");
                }
            }

            if (detectedCount > 0 || updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[CONNECTION_DETECT] Saved changes to database: {detectedCount} new, {updatedCount} updated");
                reportLines.Insert(0, $"Detected {detectedCount} new connection points, updated {updatedCount} existing");
                
                // Invalidate cache
                _cacheService.InvalidateAll();
            }
            else
            {
                reportLines.Add("No connection points detected or updated");
            }

            var report = string.Join("\n", reportLines);
            _logger.LogInformation($"[CONNECTION_DETECT] Detection completed: {detectedCount} new connection points");

            return (detectedCount, report);
        }

        /// <summary>
        /// Detects the type of connection based on node properties and connections
        /// This is a heuristic - can be overridden manually
        /// </summary>
        private string DetectConnectionType(RouteNode node, HashSet<int> connectedLevels)
        {
            var levelSpan = connectedLevels.Max() - connectedLevels.Min();
            var connectionCount = node.ConnectedNodeIds.Count;

            // Heuristics:
            // - If connects many levels at once -> elevator
            // - If connects only adjacent levels with few connections -> stairs
            // - Default to stairs if uncertain

            if (connectedLevels.Count >= 3)
            {
                // Connects 3+ levels -> likely elevator
                return "elevator";
            }
            else if (connectedLevels.Count == 2)
            {
                // Check if adjacent levels
                var levels = connectedLevels.OrderBy(l => l).ToList();
                if (levels[1] - levels[0] == 1)
                {
                    // Adjacent levels -> likely stairs or ramp
                    // If many connections, might be a large stairwell landing
                    return connectionCount > 4 ? "stairs" : "stairs";
                }
                else
                {
                    // Non-adjacent levels -> likely elevator
                    return "elevator";
                }
            }

            // Default
            return "stairs";
        }

        /// <summary>
        /// Gets routing priority for connection type (lower = preferred)
        /// </summary>
        private int GetConnectionPriority(string? connectionType)
        {
            return connectionType switch
            {
                "elevator" => 1,
                "escalator" => 1,
                "stairs" => 2,
                "ramp" => 3,
                _ => 999
            };
        }

        /// <summary>
        /// Manually marks a node as a specific type of connection point
        /// </summary>
        public async Task<bool> ManuallyMarkConnectionPointAsync(
            int nodeId, 
            string connectionType, 
            List<int>? connectedLevels = null)
        {
            var node = await _context.RouteNodes.FindAsync(nodeId);
            if (node == null)
            {
                _logger.LogWarning($"[CONNECTION_DETECT] Node {nodeId} not found");
                return false;
            }

            node.IsConnectionPoint = true;
            node.ConnectionType = connectionType;
            node.ConnectionPriority = GetConnectionPriority(connectionType);
            
            if (connectedLevels != null)
            {
                node.ConnectedLevels = connectedLevels;
            }

            node.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation($"[CONNECTION_DETECT] Manually marked node {nodeId} as {connectionType}");
            
            // Invalidate cache
            _cacheService.InvalidateConnectionPoints();
            if (node.FloorId > 0)
            {
                _cacheService.InvalidateFloor(node.FloorId);
            }

            return true;
        }

        /// <summary>
        /// Removes connection point marking from a node
        /// </summary>
        public async Task<bool> UnmarkConnectionPointAsync(int nodeId)
        {
            var node = await _context.RouteNodes.FindAsync(nodeId);
            if (node == null)
            {
                _logger.LogWarning($"[CONNECTION_DETECT] Node {nodeId} not found");
                return false;
            }

            node.IsConnectionPoint = false;
            node.ConnectionType = null;
            node.ConnectedLevels = new List<int>();
            node.ConnectionPriority = null;
            node.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            _logger.LogInformation($"[CONNECTION_DETECT] Unmarked node {nodeId} as connection point");
            
            // Invalidate cache
            _cacheService.InvalidateConnectionPoints();
            if (node.FloorId > 0)
            {
                _cacheService.InvalidateFloor(node.FloorId);
            }

            return true;
        }
    }
}
