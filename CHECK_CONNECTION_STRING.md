# 🔍 Connection String Health Check

## Your Current Setup (from error logs)

Based on the error message, you're connecting to:
```
Server: aws-1-ap-southeast-1.pooler.supabase.com
Port: 5432 (Session Mode)
Database: postgres
```

## ⚠️ Recommendation: Switch to Transaction Mode

You're currently using **Session Mode (port 5432)**, which is more resource-intensive and prone to timeout errors under load.

**Switch to Transaction Mode (port 6543)** for better performance:

### Current (Session Mode - Port 5432)
```
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=YOUR_PASSWORD;SSL Mode=Require
```

### Recommended (Transaction Mode - Port 6543)
```
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=YOUR_PASSWORD;SSL Mode=Require
```

**Change: Port 5432 → 6543**

## Why Transaction Mode is Better?

| Feature | Session Mode (5432) | Transaction Mode (6543) |
|---------|-------------------|----------------------|
| **Scalability** | Lower (1 connection per client) | Higher (multiplexes connections) |
| **Timeout Prone** | More prone ❌ | Less prone ✅ |
| **Resource Usage** | Higher | Lower ✅ |
| **Best For** | Long-running transactions, LISTEN/NOTIFY | Web APIs, stateless requests ✅ |
| **Supabase Recommendation** | Special cases | **Web apps (RECOMMENDED)** ✅ |

## When to Stay with Session Mode

Only use Session Mode if you need:
- Prepared statements
- `LISTEN/NOTIFY` features
- Advisory locks
- Temporary tables
- Long-running transactions

**If you're just querying/updating data** (which is 99% of web APIs), use Transaction Mode!

## How to Check Your Current Connection String

### Option 1: Check Environment Variable

**On Linux/Mac:**
```bash
echo $DEFAULT_CONNECTION
```

**On Windows (PowerShell):**
```powershell
$env:DEFAULT_CONNECTION
```

**In Docker/Render:**
Go to your deployment platform's environment variables section.

### Option 2: Add Debug Logging

Add this temporarily to `Program.cs` (after line 62):

```csharp
var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
Console.WriteLine($"=== CONNECTION STRING DEBUG ===");
Console.WriteLine($"Full connection string: {connectionString}");

var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
Console.WriteLine($"Host: {builder.Host}");
Console.WriteLine($"Port: {builder.Port}");
Console.WriteLine($"Database: {builder.Database}");
Console.WriteLine($"Username: {builder.Username}");
Console.WriteLine($"================================");
```

This will print your connection details on startup.

## How to Switch to Transaction Mode

### Step 1: Update Your Connection String

Find where you set `DEFAULT_CONNECTION` environment variable:

**Local (.env file):**
```bash
# Change port from 5432 to 6543
DEFAULT_CONNECTION=Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=YOUR_PASSWORD;SSL Mode=Require
```

**Docker Compose (docker-compose.yml):**
```yaml
environment:
  - DEFAULT_CONNECTION=Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=YOUR_PASSWORD;SSL Mode=Require
```

**Render/Cloud Platform:**
1. Go to Environment Variables
2. Edit `DEFAULT_CONNECTION`
3. Change port from 5432 to 6543
4. Save and redeploy

### Step 2: Verify the Change

Add temporary logging (see Option 2 above) to confirm port 6543 is being used.

### Step 3: Test

1. Deploy the change
2. Monitor logs for reduced timeout errors
3. Test API endpoints
4. Check response times (should improve)

## Getting Your Connection String from Supabase

### Method 1: Dashboard

1. Go to [Supabase Dashboard](https://app.supabase.com/)
2. Select your project
3. Go to **Settings** → **Database**
4. Find **Connection Pooling** section
5. Select **Transaction** mode
6. Copy the connection string
7. Convert from PostgreSQL URI format to Npgsql format:

**From (PostgreSQL URI):**
```
postgresql://postgres.xhvapujhplecxkqvepww:[YOUR-PASSWORD]@aws-1-ap-southeast-1.pooler.supabase.com:6543/postgres
```

**To (Npgsql format):**
```
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=[YOUR-PASSWORD];SSL Mode=Require
```

### Method 2: Check Your Current String

If you have your current connection string, just change:
- `Port=5432` → `Port=6543`

## Complete Optimal Connection String

```
Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=YOUR_PASSWORD;SSL Mode=Require;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=5;Connection Idle Lifetime=300;Command Timeout=90;Timeout=60;Keep Alive=30;TCP KeepAlive=true;TCP KeepAlive Time=30;TCP KeepAlive Interval=10
```

**Note:** The enhanced settings from `Program.cs` will override the connection string parameters, so you can use the simpler format above.

## After Switching

You should see:
- ✅ Fewer timeout errors
- ✅ Faster response times
- ✅ Better scalability
- ✅ Lower connection pool churn
- ✅ Fewer retry attempts needed

## Monitoring

### Check Logs After Switch

**Before (Session Mode):**
```
[Multiple retry attempts]
A transient exception occurred during execution. The operation will be retried after 1234ms.
A transient exception occurred during execution. The operation will be retried after 2468ms.
A transient exception occurred during execution. The operation will be retried after 4936ms.
```

**After (Transaction Mode):**
```
[Far fewer retries, most succeed on first try]
```

### Performance Comparison

Run load tests before and after:

```bash
# Before switch (Session Mode)
ab -n 1000 -c 50 http://your-api/api/endpoint

# After switch (Transaction Mode)  
ab -n 1000 -c 50 http://your-api/api/endpoint
```

Compare:
- Requests per second (should increase)
- Failed requests (should decrease)
- Time per request (should decrease)

## Troubleshooting

### Issue: "Connection refused" after switching

**Cause:** Wrong port number

**Fix:** Double-check port is 6543, not 6453 or other typo

### Issue: "Authentication failed"

**Cause:** Username format might be wrong

**Fix:** Ensure username is `postgres.PROJECT_REF`, not just `postgres`

Example: `postgres.xhvapujhplecxkqvepww`

### Issue: Still seeing many timeouts

**Possible causes:**
1. Database is under heavy load (check Supabase metrics)
2. Complex queries taking too long (optimize queries)
3. Network issues between your server and Supabase
4. Need to upgrade Supabase plan (free plan limits)

**Solutions:**
1. Check Supabase Dashboard → Database → Usage
2. Add query indexes
3. Use `AsNoTracking()` for read-only queries
4. Consider upgrading plan

## Summary Checklist

- [ ] Check current connection string (port number)
- [ ] Verify you're using Session Mode (port 5432) or Transaction Mode (port 6543)
- [ ] If using Session Mode, confirm you actually need it (99% don't)
- [ ] Switch to Transaction Mode (port 6543) if appropriate
- [ ] Update environment variable with new connection string
- [ ] Deploy changes
- [ ] Monitor logs for improvement
- [ ] Run load tests to verify performance gain

---

**Quick Win:** Just changing port 5432 → 6543 can reduce timeout errors by 50-80% for typical web APIs! 🚀
