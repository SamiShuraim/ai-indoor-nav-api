# Authentication Fix - "Tenant or user not found"

## Problem

```
Tenant or user not found
```

This error means the username/password combination is incorrect for the connection pooler.

## Solution: Force IPv4 Without Pooler (EASIEST)

Instead of using the pooler, force the direct connection to use IPv4 only:

### Connection String for Render:

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

### What Changed:
1. **Added:** `IP Address Family=IPv4` - Forces Npgsql to use IPv4 only
2. **Port:** `5432` - Direct connection (not pooler)
3. **Username:** `postgres` - Standard username (not pooler format)

This bypasses the pooler authentication issue while still forcing IPv4!

## Alternative: Get Correct Pooler Credentials from Supabase

If you want to use the pooler, get the correct connection string from Supabase:

### Steps:

1. Go to Supabase Dashboard: https://supabase.com/dashboard
2. Select your project
3. Go to **Project Settings** → **Database**
4. Scroll down to **Connection Pooling** section
5. Select **Transaction Mode**
6. Copy the connection string shown there

It will look like:
```
postgresql://postgres.[PROJECT_REF]:[PASSWORD]@aws-0-us-east-1.pooler.supabase.com:6543/postgres
```

### Convert to Npgsql Format:

If the Supabase pooler string is:
```
postgresql://postgres.xhvapujhplecxkqvepww:POOLER_PASSWORD@aws-0-us-east-1.pooler.supabase.com:6543/postgres
```

Convert it to:
```
Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=POOLER_PASSWORD;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

**Note:** The password for the pooler might be different from your direct connection password!

## Recommended Solution

**Use Option 1 (Force IPv4 with direct connection):**

```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

### Why This is Better:
- ✅ No authentication issues
- ✅ Uses your existing password
- ✅ Forces IPv4 (fixes Render issue)
- ✅ Still has connection pooling
- ✅ Simpler configuration

## Update in Render

1. Go to Render Dashboard
2. Your Service → Environment
3. Update `DEFAULT_CONNECTION` to:
   ```
   Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
   ```
4. Save and redeploy

## Testing Locally

Update your `.env` file:

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

Then run:
```bash
dotnet run
```

## If "IP Address Family=IPv4" Doesn't Work

Try using `Socket Address Family` instead:

```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;Socket Address Family=InterNetwork
```

Or set an environment variable in Render:

```
DOTNET_SYSTEM_NET_DISABLEIPV6=true
```

## Summary

**The simplest fix:**

Change your Render environment variable to:
```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

This forces IPv4 without needing the pooler's special authentication! 🎉

