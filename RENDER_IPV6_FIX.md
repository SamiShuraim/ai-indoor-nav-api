# Render.com IPv6 Issue - SOLUTION

## Problem

```
Network is unreachable [2406:da18:243:741d:edd1:a66d:5ae6:23d]:6543
```

**Root Cause:** Render.com doesn't support IPv6 outbound connections, but Supabase's hostname resolves to an IPv6 address first.

## Solution 1: Use Supabase IPv4 Pooler (RECOMMENDED)

Supabase provides IPv4-only connection pooler endpoints. Use the **Transaction Mode** pooler:

### For Render.com Environment Variable:

```
DEFAULT_CONNECTION=Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

### Key Changes:
1. **Host:** `aws-0-us-east-1.pooler.supabase.com` (IPv4-only regional pooler)
2. **Port:** `6543` (pooler port)
3. **Username:** `postgres.xhvapujhplecxkqvepww` (format: `postgres.PROJECT_REF`)
4. **Project Ref:** `xhvapujhplecxkqvepww` (extracted from your original hostname)

### Alternative Pooler Modes:

**Transaction Mode (Recommended for most apps):**
```
Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

**Session Mode (Use if you need prepared statements or LISTEN/NOTIFY):**
```
Host=aws-0-us-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

## Solution 2: Disable IPv6 in Npgsql (Alternative)

If the pooler doesn't work, force Npgsql to use IPv4 only by modifying `Program.cs`:

Add this before `builder.Services.AddDbContext`:

```csharp
// Force IPv4 only for database connections
AppContext.SetSwitch("System.Net.DisableIPv6", true);
```

Then use the original connection string:
```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

## Solution 3: Use Direct Connection with IPv4 DNS

Some cloud providers support custom DNS. Configure Render to use IPv4-only DNS:

In `render.yaml` (if you're using Infrastructure as Code):
```yaml
services:
  - type: web
    name: ai-indoor-nav-api
    env: docker
    envVars:
      - key: DEFAULT_CONNECTION
        value: Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;...
      - key: DOTNET_SYSTEM_NET_DISABLEIPV6
        value: "true"
```

Or set environment variable in Render dashboard:
```
DOTNET_SYSTEM_NET_DISABLEIPV6=true
```

## How to Apply Solution 1 (Recommended):

### Step 1: Update Environment Variable in Render

1. Go to your Render dashboard
2. Select your service
3. Go to "Environment" tab
4. Find `DEFAULT_CONNECTION` variable
5. Update it to:
   ```
   Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
   ```
6. Click "Save Changes"

### Step 2: Redeploy

Render will automatically redeploy with the new connection string.

### Step 3: Monitor Logs

Watch the deployment logs for successful connection.

## Testing Locally

Update your `.env` file to test the IPv4 pooler:

```
DEFAULT_CONNECTION=Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

Then run:
```bash
dotnet run
```

## Troubleshooting

### If IPv4 Pooler Still Fails:

1. **Verify Project Ref:** Make sure `xhvapujhplecxkqvepww` is correct
2. **Check Region:** Your Supabase project might be in a different region. Try:
   - `aws-0-us-east-1.pooler.supabase.com` (US East)
   - `aws-0-us-west-1.pooler.supabase.com` (US West)
   - `aws-0-eu-west-1.pooler.supabase.com` (EU West)
   - `aws-0-ap-southeast-1.pooler.supabase.com` (Asia Pacific)

3. **Check Supabase Dashboard:**
   - Go to Project Settings → Database
   - Look for "Connection Pooling" section
   - Copy the connection string provided there

### If Connection String Format is Wrong:

Go to your Supabase dashboard:
1. Project Settings → Database
2. Find "Connection Pooling" section
3. Select "Transaction" mode
4. Copy the connection string
5. Convert it to Npgsql format:
   - From: `postgresql://postgres.xhvapujhplecxkqvepww:[PASSWORD]@aws-0-us-east-1.pooler.supabase.com:6543/postgres`
   - To: `Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=[PASSWORD];SSL Mode=Require;Pooling=true;Maximum Pool Size=10`

## Expected Behavior After Fix

✅ Render deployment should succeed
✅ App connects to Supabase via IPv4
✅ Migrations run successfully
✅ Users are seeded
✅ API is accessible

## Why This Works

1. **IPv4-only endpoint:** The regional pooler uses IPv4 addresses
2. **Better for cloud:** Connection pooling prevents connection exhaustion
3. **Render compatible:** Works with Render's network configuration
4. **Production-ready:** Optimized for cloud deployments

## Additional Notes

- **Session Mode vs Transaction Mode:**
  - Transaction Mode (port 6543): Better for most web apps, more connections
  - Session Mode (port 5432): Required for prepared statements, LISTEN/NOTIFY

- **Username Format:**
  - Direct connection: `postgres`
  - Pooler connection: `postgres.PROJECT_REF`

- **Performance:**
  - Pooler adds minimal latency (~1-2ms)
  - Significantly better connection management
  - Recommended for all production deployments

