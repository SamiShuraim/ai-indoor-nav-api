# Render Port Binding Fix

## Problem
Render was unable to detect an open port because the application was not properly binding to the host `0.0.0.0` on the `PORT` environment variable that Render provides.

## Solution
Updated both `Program.cs` and `Dockerfile` to properly bind to the port specified by Render.

### Changes Made

#### 1. Program.cs
Added explicit port binding configuration that:
- Reads the `PORT` environment variable (Render sets this automatically)
- Defaults to port 10000 if `PORT` is not set
- Binds to `0.0.0.0` (required by Render) instead of localhost
- Logs the binding URL for debugging purposes

```csharp
// Configure URLs to bind to 0.0.0.0 on the PORT environment variable (for Render deployment)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
var urls = $"http://0.0.0.0:{port}";
Console.WriteLine($"Configuring application to listen on: {urls}");
builder.WebHost.UseUrls(urls);
```

#### 2. Dockerfile
Updated to:
- Removed hardcoded port 80 binding
- Set EXPOSE to 10000 (Render's default)
- Updated health check to use the `PORT` environment variable at runtime
- Added comment explaining the configuration

## How It Works

1. **Render sets the PORT environment variable** (default: 10000)
2. **Application reads PORT at startup** in `Program.cs`
3. **Binds to `0.0.0.0:{PORT}`** making it accessible to Render's load balancer
4. **Render detects the open port** and routes traffic to your service

## Render Requirements Met

✅ Binds to host `0.0.0.0` (not localhost)  
✅ Listens on the `PORT` environment variable  
✅ Default port is 10000 (Render's default)  
✅ Port is configurable via environment variable  

## Testing Locally

To test this locally with Render's default port:

```bash
export PORT=10000
dotnet run
```

The application should log:
```
Configuring application to listen on: http://0.0.0.0:10000
```

## Deployment

Simply push these changes and redeploy on Render. The service should now properly bind to the port and Render will detect it successfully.
