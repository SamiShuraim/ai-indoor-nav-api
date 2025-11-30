using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Models;

namespace ai_indoor_nav_api.Services
{
    public class NodeCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<NodeCacheService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Cache keys
        private const string ALL_NODES_KEY = "all_nodes";
        private const string FLOOR_NODES_PREFIX = "floor_nodes_";
        private const string CONNECTION_POINTS_KEY = "connection_points";
        private const string LEVEL_NODES_PREFIX = "level_nodes_";
        
        // Cache durations
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ConnectionPointsCacheDuration = TimeSpan.FromMinutes(30);
        
        // Statistics
        private long _cacheHits = 0;
        private long _cacheMisses = 0;

        public NodeCacheService(
            IMemoryCache cache, 
            ILogger<NodeCacheService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Gets all visible nodes, using cache if available
        /// </summary>
        public async Task<List<RouteNode>> GetAllNodesAsync()
        {
            if (_cache.TryGetValue(ALL_NODES_KEY, out List<RouteNode>? cachedNodes) && cachedNodes != null)
            {
                _cacheHits++;
                _logger.LogDebug($"[NODE_CACHE] Cache HIT for all nodes ({cachedNodes.Count} nodes)");
                return cachedNodes;
            }

            _cacheMisses++;
            _logger.LogDebug("[NODE_CACHE] Cache MISS for all nodes, fetching from database");
            
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            
            var nodes = await context.RouteNodes
                .AsNoTracking()
                .Where(n => n.IsVisible)
                .ToListAsync();

            _cache.Set(ALL_NODES_KEY, nodes, DefaultCacheDuration);
            _logger.LogInformation($"[NODE_CACHE] Cached {nodes.Count} nodes for {DefaultCacheDuration.TotalMinutes} minutes");
            
            return nodes;
        }

        /// <summary>
        /// Gets nodes on a specific floor, using cache if available
        /// </summary>
        public async Task<List<RouteNode>> GetNodesByFloorAsync(int floorId)
        {
            string cacheKey = $"{FLOOR_NODES_PREFIX}{floorId}";
            
            if (_cache.TryGetValue(cacheKey, out List<RouteNode>? cachedNodes) && cachedNodes != null)
            {
                _cacheHits++;
                _logger.LogDebug($"[NODE_CACHE] Cache HIT for floor {floorId} ({cachedNodes.Count} nodes)");
                return cachedNodes;
            }

            _cacheMisses++;
            _logger.LogDebug($"[NODE_CACHE] Cache MISS for floor {floorId}, fetching from database");
            
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            
            var nodes = await context.RouteNodes
                .AsNoTracking()
                .Where(n => n.FloorId == floorId && n.IsVisible)
                .ToListAsync();

            _cache.Set(cacheKey, nodes, DefaultCacheDuration);
            _logger.LogDebug($"[NODE_CACHE] Cached {nodes.Count} nodes for floor {floorId}");
            
            return nodes;
        }

        /// <summary>
        /// Gets nodes at a specific level, using cache if available
        /// </summary>
        public async Task<List<RouteNode>> GetNodesByLevelAsync(int level)
        {
            string cacheKey = $"{LEVEL_NODES_PREFIX}{level}";
            
            if (_cache.TryGetValue(cacheKey, out List<RouteNode>? cachedNodes) && cachedNodes != null)
            {
                _cacheHits++;
                _logger.LogDebug($"[NODE_CACHE] Cache HIT for level {level} ({cachedNodes.Count} nodes)");
                return cachedNodes;
            }

            _cacheMisses++;
            _logger.LogDebug($"[NODE_CACHE] Cache MISS for level {level}, fetching from database");
            
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            
            var nodes = await context.RouteNodes
                .AsNoTracking()
                .Where(n => n.Level == level && n.IsVisible)
                .ToListAsync();

            _cache.Set(cacheKey, nodes, DefaultCacheDuration);
            _logger.LogDebug($"[NODE_CACHE] Cached {nodes.Count} nodes for level {level}");
            
            return nodes;
        }

        /// <summary>
        /// Gets all connection points (elevator/stairs nodes), using cache if available
        /// </summary>
        public async Task<List<RouteNode>> GetConnectionPointsAsync()
        {
            if (_cache.TryGetValue(CONNECTION_POINTS_KEY, out List<RouteNode>? cachedNodes) && cachedNodes != null)
            {
                _cacheHits++;
                _logger.LogDebug($"[NODE_CACHE] Cache HIT for connection points ({cachedNodes.Count} nodes)");
                return cachedNodes;
            }

            _cacheMisses++;
            _logger.LogDebug("[NODE_CACHE] Cache MISS for connection points, fetching from database");
            
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
            
            var nodes = await context.RouteNodes
                .AsNoTracking()
                .Where(n => n.IsConnectionPoint && n.IsVisible)
                .OrderBy(n => n.ConnectionPriority ?? 999) // Prefer elevators over stairs
                .ToListAsync();

            _cache.Set(CONNECTION_POINTS_KEY, nodes, ConnectionPointsCacheDuration);
            _logger.LogInformation($"[NODE_CACHE] Cached {nodes.Count} connection points for {ConnectionPointsCacheDuration.TotalMinutes} minutes");
            
            return nodes;
        }

        /// <summary>
        /// Gets a specific node by ID, using cache if available (from all nodes cache)
        /// </summary>
        public async Task<RouteNode?> GetNodeByIdAsync(int nodeId)
        {
            var allNodes = await GetAllNodesAsync();
            var node = allNodes.FirstOrDefault(n => n.Id == nodeId);
            
            if (node != null)
            {
                _logger.LogDebug($"[NODE_CACHE] Found node {nodeId} in cache");
            }
            else
            {
                _logger.LogDebug($"[NODE_CACHE] Node {nodeId} not found in cache");
            }
            
            return node;
        }

        /// <summary>
        /// Invalidates all caches
        /// </summary>
        public void InvalidateAll()
        {
            _cache.Remove(ALL_NODES_KEY);
            _cache.Remove(CONNECTION_POINTS_KEY);
            _logger.LogInformation("[NODE_CACHE] Invalidated all node caches");
        }

        /// <summary>
        /// Invalidates cache for a specific floor
        /// </summary>
        public void InvalidateFloor(int floorId)
        {
            string cacheKey = $"{FLOOR_NODES_PREFIX}{floorId}";
            _cache.Remove(cacheKey);
            _cache.Remove(ALL_NODES_KEY); // Also invalidate all nodes since they changed
            _logger.LogInformation($"[NODE_CACHE] Invalidated cache for floor {floorId}");
        }

        /// <summary>
        /// Invalidates cache for a specific level
        /// </summary>
        public void InvalidateLevel(int level)
        {
            string cacheKey = $"{LEVEL_NODES_PREFIX}{level}";
            _cache.Remove(cacheKey);
            _cache.Remove(ALL_NODES_KEY); // Also invalidate all nodes since they changed
            _logger.LogInformation($"[NODE_CACHE] Invalidated cache for level {level}");
        }

        /// <summary>
        /// Invalidates connection points cache
        /// </summary>
        public void InvalidateConnectionPoints()
        {
            _cache.Remove(CONNECTION_POINTS_KEY);
            _logger.LogInformation("[NODE_CACHE] Invalidated connection points cache");
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public (long hits, long misses, double hitRate) GetStatistics()
        {
            long total = _cacheHits + _cacheMisses;
            double hitRate = total > 0 ? (double)_cacheHits / total * 100 : 0;
            
            _logger.LogDebug($"[NODE_CACHE] Stats - Hits: {_cacheHits}, Misses: {_cacheMisses}, Hit Rate: {hitRate:F2}%");
            
            return (_cacheHits, _cacheMisses, hitRate);
        }

        /// <summary>
        /// Resets cache statistics
        /// </summary>
        public void ResetStatistics()
        {
            _cacheHits = 0;
            _cacheMisses = 0;
            _logger.LogInformation("[NODE_CACHE] Reset cache statistics");
        }

        /// <summary>
        /// Pre-warms the cache by loading frequently used data
        /// </summary>
        public async Task PrewarmCacheAsync()
        {
            _logger.LogInformation("[NODE_CACHE] Pre-warming cache...");
            
            var allNodesTask = GetAllNodesAsync();
            var connectionPointsTask = GetConnectionPointsAsync();
            
            await Task.WhenAll(allNodesTask, connectionPointsTask);
            
            _logger.LogInformation("[NODE_CACHE] Cache pre-warming completed");
        }
    }
}
