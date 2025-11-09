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

# Install DNS utilities for debugging (optional)
RUN apt-get update && apt-get install -y dnsutils curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

# Set environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:80/api/LoadBalancer/metrics || exit 1

# Run the app
ENTRYPOINT ["dotnet", "ai-indoor-nav-api.dll"]
