# Pathfinding Algorithm Implementation

This document describes the implementation of the pathfinding algorithm for the indoor navigation API as requested in Linear issue SAM-7.

## Overview

The implementation consists of several key components:

1. **POI Model Updates**: Added `closest_node_id` and `closest_node_distance` fields to track the nearest route node for each Point of Interest (POI).

2. **NavigationService**: A service class that handles pathfinding logic and POI closest node updates.

3. **Pathfinding Endpoint**: A new API endpoint `/api/RouteNode/findPath` that accepts user location and destination POI ID to return the shortest path.

## Implementation Details

### 1. POI Model Changes

Added two new properties to the `Poi` model:
```csharp
[Column("closest_node_id")]
public int? ClosestNodeId { get; set; }

[Column("closest_node_distance")]
public double? ClosestNodeDistance { get; set; }

[ForeignKey("ClosestNodeId")]
[JsonIgnore]
public RouteNode? ClosestNode { get; set; }
```

### 2. NavigationService

The `NavigationService` class provides the following functionality:

#### UpdatePoiClosestNodesAsync
- Automatically called when a new route node is created
- Calculates distances from the new node to all POIs on the same floor
- Updates the closest node for each POI if the new node is closer

#### FindClosestNodeAsync
- Finds the closest route node to a given geographic point
- Used to determine the starting node for pathfinding based on user location

#### FindShortestPathAsync
- Implements Dijkstra's algorithm to find the shortest path between two nodes
- Returns a list of RouteNode objects representing the optimal path

### 3. Pathfinding Endpoint

**Endpoint**: `POST /api/RouteNode/findPath`

**Request Body**:
```json
{
  "userLocation": {
    "latitude": 40.7128,
    "longitude": -74.0060
  },
  "destinationPoiId": 123
}
```

**Response**: GeoJSON FeatureCollection containing:
- Path nodes as Point features with `path_order` and `is_path_node` properties
- Path edges as LineString features with `path_segment` and `is_path_edge` properties

## Algorithm Details

### Dijkstra's Algorithm Implementation

The pathfinding uses Dijkstra's algorithm with the following characteristics:

1. **Edge Weights**: Euclidean distance between connected nodes
2. **Graph Structure**: Uses the `ConnectedNodeIds` array in each RouteNode to define the graph topology
3. **Optimization**: Only considers nodes on the same floor as the start and end nodes

### Distance Calculation

Uses simple Euclidean distance formula:
```csharp
var dx = point1.X - point2.X;
var dy = point1.Y - point2.Y;
return Math.Sqrt(dx * dx + dy * dy);
```

## Usage Flow

1. **Node Creation**: When a new route node is created via `POST /api/RouteNode`, the system automatically updates the closest node for all POIs on the same floor.

2. **Path Finding**: 
   - Client sends user location and destination POI ID
   - System finds closest node to user location
   - System retrieves the pre-computed closest node for the destination POI
   - Dijkstra's algorithm calculates the shortest path
   - Returns GeoJSON with path nodes and edges

## Database Migration

A migration `AddClosestNodeToPoi` was created to add the new columns to the database:
- `closest_node_id` (nullable integer, foreign key to route_nodes)
- `closest_node_distance` (nullable double)

## Error Handling

The implementation includes comprehensive error handling:
- Validates request parameters
- Checks for POI existence
- Verifies that POI has a closest node assigned
- Handles cases where no path exists
- Returns appropriate HTTP status codes and error messages

## Testing

The implementation can be tested using the provided `TestScript.cs` which:
- Creates test data (building, floor, nodes, and POI)
- Tests POI closest node updates
- Tests pathfinding algorithm
- Validates the complete flow

## Future Enhancements

Potential improvements for the future:
1. **Multi-floor pathfinding**: Handle paths that span multiple floors
2. **A* algorithm**: More efficient pathfinding for larger graphs
3. **Real-time updates**: Handle dynamic obstacles or node availability
4. **Path optimization**: Consider factors beyond distance (e.g., accessibility, traffic)
5. **Caching**: Cache frequently requested paths for better performance

## API Documentation

The new endpoint integrates with the existing Swagger documentation and follows the same patterns as other controllers in the application.