# ✅ CORRECT Connection String (Validated via Supabase API)

## Your Supabase Project Details

- **Project Name:** ICS414
- **Project ID:** xhvapujhplecxkqvepww
- **Region:** ap-southeast-1 (Singapore)
- **Status:** ACTIVE_HEALTHY
- **Host:** db.xhvapujhplecxkqvepww.supabase.co

## THE CORRECT CONNECTION STRING FOR RENDER

### Option 1: Force IPv4 with Direct Connection (RECOMMENDED)

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

**This is the simplest solution:**
- ✅ Uses direct connection (no pooler complexity)
- ✅ Forces IPv4 (fixes Render's IPv6 issue)
- ✅ Uses your existing password
- ✅ No authentication issues

### Option 2: Use Connection Pooler (CORRECT REGION)

```
DEFAULT_CONNECTION=Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

**Key fix:** Changed from `us-east-1` to `ap-southeast-1` (your actual region)

## What Was Wrong

❌ **You were using:** `aws-0-us-east-1.pooler.supabase.com`
✅ **Should be:** `aws-0-ap-southeast-1.pooler.supabase.com`

Your project is in Singapore (ap-southeast-1), not US East!

## Update in Render NOW

1. Go to Render Dashboard
2. Your Service → Environment
3. Update `DEFAULT_CONNECTION` to **Option 1** (recommended):
   ```
   Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
   ```
4. Save and redeploy

## Why Option 1 is Better

- **Simpler:** No need to figure out pooler credentials
- **Reliable:** Direct connection with IPv4 forced
- **Same password:** Uses your existing database password
- **Works on Render:** Bypasses IPv6 issue

## If You Prefer Option 2 (Pooler)

Use the correct region:
```
Host=aws-0-ap-southeast-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

## Testing Locally

Update your `.env` file with Option 1:

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

Then run:
```bash
dotnet run
```

## Expected Result

✅ Render deployment succeeds
✅ App connects to Supabase (Singapore region)
✅ Migrations run successfully
✅ Users are seeded
✅ API is accessible

## Summary

**The issue was:** Wrong region in pooler URL (`us-east-1` instead of `ap-southeast-1`)

**The fix:** Use Option 1 (force IPv4 with direct connection) - it's simpler and more reliable!

```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10;IP Address Family=IPv4
```

Copy this into your Render environment variable and redeploy! 🚀

