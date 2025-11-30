# Transient Database Connection Error - Enhanced Retry Strategy

## Problem

The application was experiencing transient database connection timeout errors when connecting to the Supabase pooler:

```
Npgsql.NpgsqlException (0x80004005): Exception while reading from stream
  ---> System.TimeoutException: Timeout during reading attempt
```

While retry logic was already configured, the initial retry delay was 0ms, causing immediate retries without proper backoff.

## Root Causes

1. **Insufficient Connection Timeout**: 30-second timeout was too short for Supabase pooler connections under load
2. **No Initial Retry Delay**: Retries happened immediately (0ms delay) without exponential backoff
3. **No TCP Keepalive**: Connections could be dropped silently by intermediary network devices
4. **Aggressive Pooling**: Pool settings were too aggressive for pooler usage (pooler itself handles connection pooling)

## Solutions Implemented in Program.cs

### 1. Extended Timeout Settings

```csharp
// Increased from 30 to 60 seconds
Timeout = 60,

// Increased from 60 to 90 seconds  
CommandTimeout = 90,
```

**Benefits:**
- More time for pooler to establish connections under load
- Handles network latency spikes better
- Reduces false-positive timeout errors

### 2. TCP Keepalive Configuration

```csharp
KeepAlive = 30,                 // Send keepalive every 30 seconds
TcpKeepAlive = true,            // Enable TCP keepalive
TcpKeepAliveTime = 30,          // TCP keepalive time in seconds
TcpKeepAliveInterval = 10,      // TCP keepalive interval in seconds
```

**Benefits:**
- Prevents silent connection drops
- Detects dead connections faster
- Maintains connection health across NAT/firewalls

### 3. Optimized Pool Sizes for Supabase Pooler

```csharp
MaxPoolSize = 50,               // Reduced from 100 (pooler handles heavy lifting)
MinPoolSize = 5,                // Reduced from 10 (pooler pre-warms connections)
```

**Benefits:**
- Avoids overwhelming the Supabase pooler
- Pooler itself maintains a large connection pool to PostgreSQL
- More efficient resource usage on application side

### 4. Enhanced Retry Strategy with Exponential Backoff

```csharp
npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount: 6,                           // Increased from 5
    maxRetryDelay: TimeSpan.FromSeconds(30),    // Increased from 10 seconds
    errorCodesToAdd: null                       // Use default Npgsql transient error codes
);
```

**Benefits:**
- Up to 6 retry attempts (instead of 5)
- Maximum delay increased from 10s to 30s
- Exponential backoff: ~1s, ~2s, ~4s, ~8s, ~16s, ~30s
- Gives pooler more time to recover during load spikes

## How Exponential Backoff Works

EF Core's `EnableRetryOnFailure` uses exponential backoff by default:

| Attempt | Approximate Delay |
|---------|------------------|
| 1       | 0-2 seconds      |
| 2       | 2-4 seconds      |
| 3       | 4-8 seconds      |
| 4       | 8-16 seconds     |
| 5       | 16-30 seconds    |
| 6       | 30 seconds (max) |

Total maximum retry time: ~90 seconds before giving up

## Expected Behavior After Fix

✅ **Transient errors are automatically retried** with proper delays
✅ **Longer timeout windows** prevent premature failures
✅ **TCP keepalive** maintains connection health
✅ **Optimized pooling** works better with Supabase pooler
✅ **Exponential backoff** prevents retry storms

## When Errors Still Occur

If you still see these errors after implementing this fix, consider:

### 1. Check Your Connection String

Ensure you're using the **Transaction Mode pooler** (recommended):

```
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.PROJECT_REF;Password=YOUR_PASSWORD;SSL Mode=Require
```

**Not Session Mode** (port 5432):
```
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;...
```

Session Mode is more resource-intensive and prone to timeouts under load.

### 2. Verify Supabase Plan Limits

Check your Supabase dashboard:
- **Free Plan**: Limited connections and bandwidth
- **Pro Plan**: More connections, better performance
- Consider upgrading if hitting limits

### 3. Check Application Load

Monitor your application metrics:
- Concurrent request count
- Database query performance
- Connection pool utilization
- Network latency to Supabase region

### 4. Consider Using Direct Connection for Background Jobs

For long-running background tasks, consider using direct connection:

```csharp
// For background services that need long-running connections
builder.Services.AddDbContext<BackgroundDbContext>(options =>
    options.UseNpgsql(
        directConnectionString,  // Not the pooler
        npgsqlOptions => {
            npgsqlOptions.UseNetTopologySuite();
            npgsqlOptions.CommandTimeout(300);  // 5 minutes for long operations
        }
    )
);
```

### 5. Add Connection Health Checks

Monitor connection health:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionStringBuilder.ToString());

// Then expose endpoint
app.MapHealthChecks("/health");
```

## Monitoring Recommendations

### Add Logging for Retry Attempts

In `appsettings.json` (or `appsettings.Production.json`):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Connection": "Information"
    }
  }
}
```

This will log retry attempts so you can monitor:
- How often retries occur
- Which queries are timing out
- Whether the retry strategy is effective

### Metrics to Track

1. **Connection Pool Metrics**:
   - Active connections
   - Idle connections
   - Wait time for connections

2. **Query Performance**:
   - Average query execution time
   - 95th percentile query time
   - Timeout frequency

3. **Error Rates**:
   - Transient errors per minute
   - Successful retry rate
   - Failed after all retries

## Alternative: Switch to Transaction Mode Pooler

If you're currently using Session Mode (port 5432), switch to Transaction Mode (port 6543):

**Why Transaction Mode is Better for Web Apps:**
- Lower resource usage per connection
- Better scalability under load
- Faster connection acquisition
- Recommended by Supabase for stateless APIs

**When to Use Session Mode:**
- You need prepared statements
- You use `LISTEN/NOTIFY`
- You need advisory locks
- You use temporary tables

**To Switch:**

Update your `DEFAULT_CONNECTION` environment variable:
```
# Change port from 5432 to 6543
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.PROJECT_REF;Password=YOUR_PASSWORD;SSL Mode=Require
```

## Testing the Fix

### 1. Simulate Load Locally

```bash
# Install Apache Bench
sudo apt install apache2-utils

# Test with 50 concurrent requests
ab -n 1000 -c 50 http://localhost:5090/api/routenode/find-path
```

### 2. Monitor Logs

Watch for retry messages:
```bash
# In production/Docker
docker logs -f YOUR_CONTAINER_NAME

# Look for:
# "A transient exception occurred during execution. The operation will be retried after XXXms."
```

### 3. Check Success Rate

After load testing, verify:
- ✅ Most requests succeed after 1-2 retries
- ✅ Total error rate is low (<1%)
- ✅ No cascade failures

## Performance Impact

### Before Fix
- ❌ Timeout after 30 seconds
- ❌ Immediate retries (0ms delay)
- ❌ Only 5 retry attempts
- ❌ No TCP keepalive
- ❌ Aggressive pooling could overwhelm pooler

### After Fix
- ✅ Timeout after 60 seconds (2x longer)
- ✅ Exponential backoff (1s to 30s)
- ✅ 6 retry attempts with longer delays
- ✅ TCP keepalive prevents silent drops
- ✅ Optimized pooling for Supabase pooler
- ✅ Total retry window: ~90 seconds

## Related Documentation

- `DATABASE_TIMEOUT_FIX.md` - Original timeout fix documentation
- `USE_SUPABASE_POOLER.md` - Supabase pooler configuration guide
- [Npgsql Connection Pooling](https://www.npgsql.org/doc/connection-string-parameters.html)
- [EF Core Connection Resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)
- [Supabase Pooler Modes](https://supabase.com/docs/guides/database/connecting-to-postgres#connection-pooler)

## Summary

This fix enhances the existing retry strategy with:
1. **Longer timeouts** (60s connection, 90s command)
2. **Better exponential backoff** (up to 30s max delay)
3. **TCP keepalive** (detect dead connections)
4. **Optimized pooling** (50 max, 5 min for pooler usage)
5. **6 retry attempts** (instead of 5)

The application will now be more resilient to transient network issues and Supabase pooler load spikes.
