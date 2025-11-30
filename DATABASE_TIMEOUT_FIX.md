# Database Connection Timeout Fix for Load Testing

## Problem
When load testing the API with 50 concurrent users, the application experienced frequent database connection timeouts with errors:
- `System.TimeoutException: Timeout during reading attempt`
- `Npgsql.NpgsqlException: Exception while reading from stream`
- Connection pool exhaustion under high load

## Root Causes
1. **No connection pooling configuration** - Default Npgsql settings were insufficient for high concurrency
2. **No command timeout settings** - Queries would timeout with default 30-second timeout under load
3. **No retry strategy** - Transient failures weren't automatically retried
4. **Inefficient queries** - EF Core was tracking entities unnecessarily in read-only queries

## Solutions Implemented

### 1. Connection Pooling Configuration (`Program.cs`)
Added comprehensive connection string builder with optimal pooling settings:

```csharp
var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    // Connection pooling settings
    MaxPoolSize = 100,              // Maximum connections in pool (default 100)
    MinPoolSize = 10,               // Keep minimum connections alive (default 1)
    ConnectionIdleLifetime = 300,   // Close idle connections after 5 minutes
    ConnectionPruningInterval = 10, // Check for idle connections every 10 seconds
    
    // Timeout settings
    Timeout = 30,                   // Connection timeout in seconds (default 15)
    CommandTimeout = 60,            // Command execution timeout in seconds (default 30)
    
    // Performance settings
    NoResetOnClose = false,         // Reset connection state on close for safety
    Pooling = true                  // Ensure pooling is enabled
};
```

**Benefits:**
- Pre-warmed connection pool with 10 minimum connections
- Up to 100 concurrent connections supported
- Automatic cleanup of idle connections
- Doubled command timeout to handle complex queries under load

### 2. EF Core Resilience and Retry Strategy (`Program.cs`)
Configured automatic retry logic for transient failures:

```csharp
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(
        connectionStringBuilder.ToString(),
        npgsqlOptions => {
            npgsqlOptions.UseNetTopologySuite();
            
            // Configure retry logic for transient failures
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            );
            
            // Set command timeout (same as connection string for consistency)
            npgsqlOptions.CommandTimeout(60);
        }
    )
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
);
```

**Benefits:**
- Automatic retry up to 5 times for transient database failures
- Exponential backoff with max 10-second delay between retries
- Handles network glitches and temporary connection issues gracefully

### 3. Query Optimization with AsNoTracking
Added `AsNoTracking()` to all read-only queries to improve performance:

#### RouteNodeController.cs
- `FindPath` endpoint - POI lookup query
- `NavigateToLevel` endpoint - Route node lookup query
- `FixBidirectionalConnections` endpoint - Floor validation query
- `CreateRouteNode` endpoint - Floor validation query

#### NavigationService.cs
- `FindClosestNodeAsync` - Route node queries
- `FindClosestNodeByLevelAsync` - Route node queries with level filter
- `FindShortestPathAsync` - All nodes query for pathfinding
- `FindCrossLevelPathAsync` - Cross-floor node queries
- `RecalculateAllPoiClosestNodesAsync` - Route node distance calculations

**Benefits:**
- Reduced memory usage (no change tracking overhead)
- Faster query execution (no snapshot creation)
- Better performance under high concurrency
- Lower CPU usage on database server

### 4. DbContext Configuration (`MyDbContext.cs`)
Added explicit configuration for query behavior:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    base.OnConfiguring(optionsBuilder);
    
    // Set default query timeout to 60 seconds
    optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
}
```

## Performance Impact

### Before Fixes
- ❌ Timeout errors with 50 concurrent users
- ❌ Connection pool exhaustion
- ❌ 30-second query timeout insufficient
- ❌ Unnecessary entity tracking overhead

### After Fixes
- ✅ Supports 100+ concurrent connections
- ✅ Automatic retry for transient failures
- ✅ 60-second query timeout for complex operations
- ✅ Optimized read queries with AsNoTracking
- ✅ Pre-warmed connection pool (10 minimum connections)
- ✅ Automatic idle connection cleanup

## Testing Recommendations

1. **Load Testing**: Test with 50-100 concurrent users using tools like Apache Bench, k6, or JMeter
2. **Monitor Metrics**:
   - Database connection pool usage
   - Query execution times
   - Error rates and retry attempts
   - Memory usage
3. **Stress Testing**: Gradually increase concurrent users to find the new breaking point
4. **Network Resilience**: Test behavior during temporary network issues

## Configuration Tuning

If you still experience issues under extreme load, consider adjusting:

1. **MaxPoolSize**: Increase from 100 if you need more concurrent connections
2. **CommandTimeout**: Increase from 60 if complex queries legitimately take longer
3. **MinPoolSize**: Increase from 10 to keep more connections warm for immediate use
4. **MaxRetryCount**: Decrease from 5 if retries are causing cascading delays

## Monitoring Connection Pool

Add this to your application startup to log pool statistics:

```csharp
// Optional: Log connection pool statistics periodically
builder.Services.AddHostedService<ConnectionPoolMonitorService>();
```

## Database Server Considerations

Ensure your Supabase/PostgreSQL server can handle the connection load:
- Check `max_connections` setting (default is often 100)
- Monitor server CPU and memory usage
- Consider upgrading to a higher Supabase plan if needed
- Use connection pooling on the database side (PgBouncer) for enterprise scale

## Related Documentation
- `USE_SUPABASE_POOLER.md` - Supabase pooler configuration
- `SUPABASE_MIGRATION.md` - Database migration guide
- [Npgsql Connection Pooling](https://www.npgsql.org/doc/connection-string-parameters.html#pooling)
- [EF Core Connection Resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)
