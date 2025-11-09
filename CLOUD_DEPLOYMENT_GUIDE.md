# Cloud Deployment Guide

## Issues Identified

### 1. DNS Resolution Error in Cloud
```
System.Net.Sockets.SocketException: Name or service not known
```

This error occurs because the Docker container in your cloud environment cannot resolve the Supabase hostname `db.xhvapujhplecxkqvepww.supabase.co`.

### 2. Missing Environment Variables
The `.env` file is not included in the Docker image (and shouldn't be for security reasons).

## Solutions

### Option 1: Use Supabase Connection Pooler (Recommended for Cloud)

Supabase provides a connection pooler (PgBouncer) that's more reliable for cloud deployments:

**Connection String (IPv4 + IPv6):**
```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

**Connection String (IPv4 Only - Use this for Render.com):**
```
Host=aws-0-us-east-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.xhvapujhplecxkqvepww;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

**Key Changes:**
- Port changed from `5432` to `6543` (connection pooler port)
- For IPv4-only environments (like Render), use the regional pooler URL
- Username format changes to `postgres.PROJECT_REF` for regional pooler
- Added `Pooling=true` and `Maximum Pool Size=10`

### Option 2: Use IPv6 Address Directly

If DNS resolution fails, use the IPv6 address directly:

**Connection String:**
```
Host=2406:da18:243:741d:edd1:a66d:5ae6:23d;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require
```

**Note:** This is less ideal as the IP may change, but it bypasses DNS issues.

### Option 3: Configure DNS in Docker

Add DNS configuration to your Dockerfile or docker-compose:

```dockerfile
# In Dockerfile, before ENTRYPOINT
RUN echo "nameserver 8.8.8.8" > /etc/resolv.conf
RUN echo "nameserver 8.8.4.4" >> /etc/resolv.conf
```

Or in docker-compose.yml:
```yaml
services:
  api:
    dns:
      - 8.8.8.8
      - 8.8.4.4
```

## Required Environment Variables for Cloud Deployment

Set these environment variables in your cloud hosting service (e.g., Render, Railway, Azure, AWS, etc.):

### Database Connection
```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

### JWT Configuration
```
JWT_ISSUER=your-issuer-here
JWT_AUDIENCE=your-audience-here
JWT_KEY=your-minimum-32-character-secret-key-here
```

### User Configuration
```
USER1_USERNAME=admin
USER1_EMAIL=admin@example.com
USER1_PASSWORD=SecurePassword123!

USER2_USERNAME=user2
USER2_EMAIL=user2@example.com
USER2_PASSWORD=SecurePassword123!

USER3_USERNAME=user3
USER3_EMAIL=user3@example.com
USER3_PASSWORD=SecurePassword123!
```

## Platform-Specific Instructions

### Render.com
1. Go to your service dashboard
2. Navigate to "Environment" tab
3. Add each environment variable as a key-value pair
4. Redeploy the service

### Railway.app
1. Go to your project
2. Click on "Variables" tab
3. Add each environment variable
4. Railway will automatically redeploy

### Heroku
```bash
heroku config:set DEFAULT_CONNECTION="Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;..."
heroku config:set JWT_ISSUER="your-issuer"
# ... etc
```

### Azure App Service
1. Go to Configuration → Application settings
2. Add each environment variable
3. Save and restart the app

### AWS Elastic Beanstalk
1. Go to Configuration → Software
2. Add environment properties
3. Apply changes

## Testing Connection Locally with Pooler

Update your `.env` file to test the pooler connection:

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

Then run:
```bash
dotnet run
```

## Dockerfile Best Practices

### Current Dockerfile Issues:
1. ❌ Doesn't copy `.env` file (correct - shouldn't be in image)
2. ❌ Exposes port 80 but app runs on 5090
3. ❌ No health check configured

### Improved Dockerfile:

```dockerfile
# Stage 1: Build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY ai-indoor-nav-api.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Stage 2: Run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install DNS utilities (optional, for debugging)
RUN apt-get update && apt-get install -y dnsutils iputils-ping && rm -rf /var/lib/apt/lists/*

# Configure DNS (use Google's DNS)
RUN echo "nameserver 8.8.8.8" > /etc/resolv.conf && \
    echo "nameserver 8.8.4.4" >> /etc/resolv.conf

COPY --from=build /app/out .

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/api/LoadBalancer/metrics || exit 1

# Run the app
ENTRYPOINT ["dotnet", "ai-indoor-nav-api.dll"]
```

## Troubleshooting

### If DNS Still Fails:

1. **Check if IPv6 is supported** in your cloud environment:
   ```bash
   # In container
   ping6 2406:da18:243:741d:edd1:a66d:5ae6:23d
   ```

2. **Use IPv4 if available**: Contact Supabase support to get IPv4 address

3. **Use Supabase API URL**: Some cloud providers block direct database connections. Consider using Supabase's REST API or GraphQL API instead.

### If Connection Pooler Fails:

Try the direct connection (port 5432) but with connection pooling in the connection string:
```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=10;Connection Idle Lifetime=300
```

### If Migrations Fail on Startup:

Consider running migrations separately before deployment:
1. Comment out the migration code in `Program.cs` (lines 99-103)
2. Run migrations manually using a local connection or a migration job
3. Deploy the app without automatic migrations

## Security Recommendations

1. **Never commit `.env` file** to version control
2. **Use different passwords** for production
3. **Rotate JWT keys** regularly
4. **Use secrets management** (Azure Key Vault, AWS Secrets Manager, etc.)
5. **Enable Supabase Row Level Security (RLS)** for additional protection
6. **Use connection pooling** to prevent connection exhaustion
7. **Set up monitoring** for database connections and performance

## Next Steps

1. ✅ Update `.env` file locally to use connection pooler (port 6543)
2. ✅ Test locally with `dotnet run`
3. ✅ Set environment variables in your cloud hosting service
4. ✅ Update Dockerfile with improved version
5. ✅ Redeploy to cloud
6. ✅ Monitor logs for any connection issues
7. ✅ Set up proper JWT keys and user credentials for production

