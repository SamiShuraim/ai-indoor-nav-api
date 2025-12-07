# Synchronous Command Execution Error Fix

## Problem

The application was throwing the following error when multiplexing was enabled:

```
System.NotSupportedException: Synchronous command execution is not supported when multiplexing is on
```

### Root Cause

When Npgsql multiplexing is enabled in the connection string (line 86 in `Program.cs`):
```csharp
Multiplexing = true,  // Enable multiplexing for transaction pooling
```

Npgsql does **NOT** allow synchronous database operations. The error occurred at:

**File:** `Controllers/RouteNodeController.cs`  
**Line:** 55 (before fix)

```csharp
return Ok(query.ToGeoJsonFeatureCollection());
```

The problem was that `query` was an `IQueryable<RouteNode>` (a database query that hadn't been executed yet), and `ToGeoJsonFeatureCollection()` attempted to enumerate it synchronously using `foreach`, which triggered the error.

### Why Multiplexing Requires Async?

Multiplexing allows multiple logical commands to share a single physical database connection, improving performance. However, this requires all operations to be asynchronous to avoid blocking the shared connection.

## Solution

Materialize the query using `ToListAsync()` **before** passing it to `ToGeoJsonFeatureCollection()`:

```csharp
// Materialize the query before converting to GeoJSON (required for multiplexing)
var routeNodes = await query.ToListAsync();
return Ok(routeNodes.ToGeoJsonFeatureCollection());
```

### What Changed?

1. Added `await query.ToListAsync()` to execute the database query asynchronously
2. Stored the result in `routeNodes` variable
3. Passed the materialized list (not the query) to `ToGeoJsonFeatureCollection()`

Now the database operation is async, and the enumeration happens on an in-memory list rather than a database query.

## Verification

I checked all other controllers for similar issues:

✅ **PoiController.cs** (line 37) - Already using `await query.ToListAsync()` before conversion  
✅ **BeaconController.cs** (lines 30, 57) - Already using `await query.ToListAsync()` before conversion  
✅ **RouteNodeController.cs** (line 55) - **FIXED** ✓

## Why Other Controllers Didn't Fail?

The other controllers (PoiController, BeaconController) were already properly materializing queries before converting to GeoJSON, so they weren't affected by this issue.

## Testing

After this fix, the following endpoint should work correctly:

```
GET /api/RouteNode?floor=1
GET /api/RouteNode?building=1
GET /api/RouteNode
```

## Related Configuration

This fix is specifically required because of the following Npgsql configuration in `Program.cs`:

```csharp
var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    Multiplexing = true,      // Requires async operations
    NoResetOnClose = true,    // Required for transaction pooling
    Pooling = true,           // Enable client-side pooling
    MaxPoolSize = 50,
    MinPoolSize = 10,
    Timeout = 60,
    CommandTimeout = 60,
};
```

## Best Practices

When using Npgsql multiplexing, always:

1. ✅ Use `ToListAsync()`, `FirstOrDefaultAsync()`, `AnyAsync()`, etc. instead of synchronous versions
2. ✅ Materialize queries before passing to extension methods that enumerate
3. ✅ Ensure all database operations are `async`/`await`
4. ❌ Never use `.ToList()`, `.First()`, `.Any()`, etc. on `IQueryable` (database queries)
5. ❌ Never use synchronous enumeration (`foreach`) on unevaluated queries

## Additional Notes

- The fix maintains the same functionality while making it compatible with multiplexing
- No changes to the `GeoJsonExtensions.cs` file were needed
- All other controllers were already following the correct pattern
- The cache service and navigation service were also verified to use async operations correctly
