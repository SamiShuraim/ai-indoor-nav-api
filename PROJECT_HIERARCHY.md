# Project Hierarchy

## AI Indoor Navigation API

This is an ASP.NET Core Web API for indoor navigation with load balancing capabilities.

### Database Configuration

**Database Provider:** PostgreSQL (Supabase)
- **Host:** db.xhvapujhplecxkqvepww.supabase.co
- **Port:** 5432
- **Database:** postgres
- **Connection String Format:** Stored in `.env` file as `DEFAULT_CONNECTION`

**Extensions:**
- PostGIS (for spatial data support)

### Project Structure

```
ai-indoor-nav-api/
├── Controllers/           # API Controllers
│   ├── BeaconController.cs
│   ├── BeaconTypeController.cs
│   ├── BuildingController.cs
│   ├── FloorController.cs
│   ├── LoadBalancerController.cs
│   ├── LoginController.cs
│   ├── PoiCategoryController.cs
│   ├── PoiController.cs
│   └── RouteNodeController.cs
│
├── Data/                  # Database Context
│   └── MyDbContext.cs
│
├── Enums/                 # Enumerations
│   └── PoiType.cs
│
├── Migrations/            # EF Core Migrations
│   ├── 20250810163652_Initial.cs
│   ├── 20250811211906_POI_props_turned_snake_case.cs
│   ├── 20250811212352_rest_converted_from_snake_case.cs
│   ├── 20250915220004_AddPoiClosestNode.cs
│   ├── 20250921180500_ResetAutoIncrementSequences.cs
│   ├── 20251109174941_AddLevelToRouteNode.cs
│   └── MyDbContextModelSnapshot.cs
│
├── Models/                # Data Models
│   ├── AddConnectionRequest.cs
│   ├── ArrivalAssignRequest.cs
│   ├── ArrivalAssignResponse.cs
│   ├── Beacon.cs
│   ├── Building.cs
│   ├── ConfigUpdateRequest.cs
│   ├── FixConnectionsRequest.cs
│   ├── Floor.cs
│   ├── LoginRequest.cs
│   ├── MetricsResponse.cs
│   ├── Node.cs
│   ├── PathRequest.cs
│   └── Poi.cs
│
├── Services/              # Business Logic Services
│   ├── AssignmentLog.cs
│   ├── LoadBalancerConfig.cs
│   ├── LoadBalancerService.cs
│   ├── NavigationService.cs
│   ├── RollingCounts.cs
│   └── RollingQuantileEstimator.cs
│
├── Util/                  # Utility Classes
│   ├── FeatureConverter.cs
│   ├── GeoJsonExtensions.cs
│   └── RequestParser.cs
│
├── .env                   # Environment Variables (not in git)
├── appsettings.json       # Application Settings
├── appsettings.Development.json
├── Program.cs             # Application Entry Point
├── Dockerfile             # Docker Configuration
└── README.md              # Project Documentation
```

### Database Schema

**Core Tables:**
- `buildings` - Building information
- `floors` - Floor information linked to buildings
- `poi` (Points of Interest) - Locations of interest
- `poi_categories` - Categories for POIs
- `route_nodes` - Navigation nodes with spatial data
- `beacons` - Beacon devices for indoor positioning
- `beacon_types` - Types of beacon devices

**Identity Tables:**
- `AspNetUsers` - User accounts
- `AspNetRoles` - User roles
- `AspNetUserRoles` - User-role relationships
- `AspNetUserClaims` - User claims
- `AspNetRoleClaims` - Role claims
- `AspNetUserLogins` - External login providers
- `AspNetUserTokens` - Authentication tokens

### Recent Changes (2025-11-09)

1. **Database Migration to Supabase**
   - Migrated from local PostgreSQL to Supabase cloud database
   - Updated `.env` file with connection pooler (port 6543)
   - Applied all existing migrations to new database

2. **Fixed ModelSnapshot Exclusion**
   - Removed `MyDbContextModelSnapshot.cs` from compilation exclusions in `.csproj`
   - This was preventing proper migration generation

3. **Added Level Column to RouteNodes**
   - Applied `AddLevelToRouteNode` migration
   - Added `level` column to `route_nodes` table for multi-level navigation support

4. **Improved Docker Configuration**
   - Added DNS utilities for debugging
   - Added health check endpoint
   - Configured for production environment
   - Added connection pooling support

5. **Cloud Deployment Support**
   - Created comprehensive deployment guide
   - Configured connection pooler for better cloud reliability
   - Added environment variable documentation

### Environment Variables

The following environment variables must be set in the `.env` file:

```
DEFAULT_CONNECTION=Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require
JWT_ISSUER=your-issuer
JWT_AUDIENCE=your-audience
JWT_KEY=your-secret-key-here-minimum-32-characters-long-for-security
USER1_USERNAME=user1
USER1_EMAIL=user1@example.com
USER1_PASSWORD=Password123!
USER2_USERNAME=user2
USER2_EMAIL=user2@example.com
USER2_PASSWORD=Password123!
USER3_USERNAME=user3
USER3_EMAIL=user3@example.com
USER3_PASSWORD=Password123!
```

### Key Features

1. **Indoor Navigation**
   - Pathfinding between points of interest
   - Multi-level navigation support
   - Spatial queries using PostGIS

2. **Load Balancing**
   - Adaptive load balancer for pilgrim distribution
   - Capacity-based assignment with soft gates
   - Dynamic age cutoff calculation
   - Utilization feedback controller

3. **Beacon Management**
   - Track beacon devices
   - Monitor battery levels and status
   - Support for multiple beacon types

4. **Authentication**
   - JWT-based authentication
   - ASP.NET Core Identity integration
   - Role-based authorization

### Running the Application

```bash
# Restore dependencies
dotnet restore

# Apply migrations (if needed)
dotnet ef database update

# Run the application
dotnet run
```

The API will be available at `http://localhost:5090` (or as configured in `appsettings.Development.json`).

### API Documentation

When running in development mode, Swagger documentation is available at:
- `http://localhost:5090/swagger`

