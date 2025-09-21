# ID Auto-Increment Fix

## Problem Fixed

The application was allowing the frontend to specify ID values when creating new records, which is a security and data integrity issue. The database should automatically generate unique ID values for all new records.

## Changes Made

### 1. Model Changes
- Updated all model ID properties from `{ get; set; }` to `{ get; init; }` to prevent manual assignment after object creation
- Affected models:
  - `Building.Id`
  - `Floor.Id` 
  - `Beacon.Id`
  - `BeaconType.Id`
  - `Poi.Id` (already had `init`)
  - `PoiCategory.Id` (already had `init`)
  - `RouteNode.Id`

### 2. Controller Changes
- Modified all POST endpoints to ignore incoming ID values and create new objects with only the necessary properties
- Updated controllers:
  - `BuildingController.PostBuilding()`
  - `FloorController.PostFloor()`
  - `BeaconTypeController.PostBeaconType()`
  - `PoiCategoryController.PostPoiCategory()`

### 3. RequestParser Changes
- Updated `GeoJsonExtensions.FromFlattened()` method to skip ID properties when parsing JSON
- Updated `GeoJsonExtensions.PopulateFromJson()` method to skip ID properties when updating objects
- This affects controllers that use `RequestParser`:
  - `BeaconController.PostBeacon()`
  - `PoiController.PostPoi()`
  - `RouteNodeController.CreateRouteNode()`

### 4. Database Migration
- Created migration `20250921180500_ResetAutoIncrementSequences`
- Contains ONLY the SQL commands to reset auto-increment sequences to ensure they're at the correct values
- This handles cases where existing records might have manually assigned IDs

## Migration Instructions

### To Apply the Changes:

1. **Apply the database migration:**
   ```bash
   dotnet ef database update
   ```

2. **If you have an existing database with data:**
   The migration includes sequence reset commands that will automatically adjust the auto-increment sequences to start from the correct value based on existing data.

3. **Verify the changes:**
   - Test creating new records via the API
   - Confirm that IDs are automatically generated
   - Ensure frontend cannot override ID values

### Expected Behavior After Fix:

- ✅ All new records will have automatically generated IDs
- ✅ Frontend cannot specify ID values when creating records
- ✅ Existing records are unaffected
- ✅ Auto-increment sequences start from the correct values
- ✅ Database integrity is maintained

### Tables Affected:
- `buildings`
- `floors` 
- `beacons`
- `beacon_types`
- `poi`
- `poi_categories`
- `route_nodes`

## Testing

Test each POST endpoint to ensure:
1. New records are created successfully
2. IDs are automatically assigned by the database
3. Returned objects have valid auto-generated IDs
4. Frontend cannot override the ID generation

### Sample Test Commands:

```bash
# Test Building creation (should ignore any ID in request)
curl -X POST "http://localhost:5000/api/Building" \
  -H "Content-Type: application/json" \
  -d '{"id": 999, "name": "Test Building", "description": "This ID should be ignored"}'

# Test Floor creation
curl -X POST "http://localhost:5000/api/Floor" \
  -H "Content-Type: application/json" \
  -d '{"id": 999, "name": "Test Floor", "floorNumber": 1, "buildingId": 1}'

# Test Beacon creation (GeoJSON format)
curl -X POST "http://localhost:5000/api/Beacon" \
  -H "Content-Type: application/json" \
  -d '{
    "type": "Feature",
    "geometry": {"type": "Point", "coordinates": [-122.4194, 37.7749]},
    "properties": {"id": 999, "name": "Test Beacon", "floorId": 1}
  }'
```

In all cases, the returned object should have an auto-generated ID, not the ID specified in the request.