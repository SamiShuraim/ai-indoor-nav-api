# Level-to-Level Navigation API

## Overview
This endpoint enables cross-level navigation for indoor positioning. After a user's position is trilaterated and they are assigned a level, this API provides a route to navigate from their current level to a target level.

## Endpoint

### Navigate to Level
**POST** `/api/RouteNode/navigateToLevel`

Finds the shortest path from the user's current position to a target level, handling level transitions (stairs, elevators, etc.).

**Works for both:**
- **Same-level routing**: From current position to a node on the same level
- **Cross-level routing**: From current position (any level or no level) to a target level

**Important**: The user's current position does NOT need to have a level assigned. The endpoint will find the closest node to the user's position regardless of level, then route to the target level.

#### Request Body
```json
{
  "currentPosition": {
    "latitude": 21.4225,
    "longitude": 39.8262
  },
  "targetLevel": 3,
  "floorId": 1
}
```

#### Request Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `currentPosition` | `LocationPoint` | Yes | User's current GPS coordinates (can be at any level or no level) |
| `currentPosition.latitude` | `double` | Yes | Latitude coordinate |
| `currentPosition.longitude` | `double` | Yes | Longitude coordinate |
| `targetLevel` | `int` | Yes | Target level to navigate to (1, 2, 3, etc.) |
| `floorId` | `int` | No | Floor ID (defaults to 1 if not provided) |

#### Response
Returns a GeoJSON `FeatureCollection` containing:
- **Point Features**: Path nodes with metadata
- **LineString Features**: Path segments connecting nodes

##### Response Structure (200 OK)
```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "Point",
        "coordinates": [39.8262, 21.4225]
      },
      "properties": {
        "id": 101,
        "floor_id": 1,
        "level": 1,
        "path_order": 0,
        "is_path_node": true,
        "node_level": 1
      }
    },
    {
      "type": "Feature",
      "geometry": {
        "type": "Point",
        "coordinates": [39.8263, 21.4226]
      },
      "properties": {
        "id": 102,
        "floor_id": 1,
        "level": 2,
        "path_order": 1,
        "is_path_node": true,
        "node_level": 2,
        "is_level_transition": true,
        "transition_from_level": 1,
        "transition_to_level": 2
      }
    },
    {
      "type": "Feature",
      "geometry": {
        "type": "LineString",
        "coordinates": [
          [39.8262, 21.4225],
          [39.8263, 21.4226]
        ]
      },
      "properties": {
        "path_segment": 0,
        "is_path_edge": true,
        "from_node_id": 101,
        "to_node_id": 102,
        "from_level": 1,
        "to_level": 2,
        "is_level_transition": true
      }
    }
  ]
}
```

##### Feature Properties

**Point Feature Properties:**
- `path_order`: Order of the node in the path (0-indexed)
- `is_path_node`: Always `true` for path nodes
- `node_level`: The level of this node
- `is_level_transition`: `true` if this node represents a level change
- `transition_from_level`: Previous level (only present for transitions)
- `transition_to_level`: New level (only present for transitions)

**LineString Feature Properties:**
- `path_segment`: Segment index (0-indexed)
- `is_path_edge`: Always `true` for path edges
- `from_node_id`: Starting node ID
- `to_node_id`: Ending node ID
- `from_level`: Starting node's level
- `to_level`: Ending node's level
- `is_level_transition`: `true` if this segment crosses levels

#### Error Responses

**400 Bad Request**
```json
{
  "error": "Current position and target level are required."
}
```

**404 Not Found**
```json
{
  "error": "No route nodes found on floor 1."
}
```
or
```json
{
  "error": "No path found from current position to level 3."
}
```

**500 Internal Server Error**
```json
{
  "error": "Internal server error: [error message]"
}
```

## Algorithm

### A* Pathfinding with Level Transitions
The endpoint uses an A* algorithm optimized for cross-level navigation:

1. **Heuristic Function**: Uses level difference as the heuristic
   - Each level difference adds a cost of 10.0 to the heuristic
   - Nodes without level information get a penalty of 100.0

2. **Level Transition Penalty**: 
   - A penalty of 50.0 is added when transitioning between levels
   - This ensures the algorithm prefers staying on the same level when possible

3. **Node Grouping Optimization**:
   - Nodes are grouped by level for efficient lookup
   - Only visible nodes are considered in pathfinding

### How It Works

1. **Find Start Node**: Locates the nearest node to the user's current position
   - **No level filtering**: Finds closest node regardless of level
   - Handles users at random positions without level assignment
   - Works even if the start node has no level assigned
   
2. **Cross-Level Pathfinding**: Uses A* to find the shortest path considering:
   - Physical distance between nodes
   - Level transition costs
   - Level difference heuristic
   - Gracefully handles nodes without level information
   
3. **Path Construction**: Returns the complete path with all nodes and edges
   - If already at target level, returns the current node
   - Otherwise returns full path to target level
   
4. **Transition Marking**: Identifies and marks level transitions (stairs/elevators)

## Usage Example

### Mobile App Integration

```javascript
// After trilateration (user position is known, but may not have level assigned)
const currentPosition = {
  latitude: 21.4225,
  longitude: 39.8262
};

const targetLevel = 3;  // User wants to go to level 3
const floorId = 1;      // Optional, but recommended

// Make API request
// NOTE: No currentLevel needed! The endpoint finds the closest node automatically
const response = await fetch('https://your-api.com/api/RouteNode/navigateToLevel', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    currentPosition,
    targetLevel,
    floorId
  })
});

const pathData = await response.json();

// Process the path
pathData.features.forEach(feature => {
  if (feature.properties.is_path_node) {
    // Draw node on map
    if (feature.properties.is_level_transition) {
      // Highlight this as a stair/elevator
      console.log(`Level transition: ${feature.properties.transition_from_level} → ${feature.properties.transition_to_level}`);
    }
  } else if (feature.properties.is_path_edge) {
    // Draw path segment
    if (feature.properties.is_level_transition) {
      // Draw this segment differently (e.g., dashed line)
    }
  }
});
```

### Use Cases

#### 1. User at Random Position (No Level)
```javascript
// User just trilaterated, position known but not assigned to a level yet
fetch('/api/RouteNode/navigateToLevel', {
  method: 'POST',
  body: JSON.stringify({
    currentPosition: { latitude: 21.4225, longitude: 39.8262 },
    targetLevel: 2
  })
});
// → Finds closest node (any level), then routes to level 2
```

#### 2. Same-Level Routing
```javascript
// User is on level 1, wants to go to a different location on level 1
fetch('/api/RouteNode/navigateToLevel', {
  method: 'POST',
  body: JSON.stringify({
    currentPosition: { latitude: 21.4225, longitude: 39.8262 },
    targetLevel: 1
  })
});
// → If closest node is already on level 1, may return minimal path or just target node
```

#### 3. Multi-Level Routing
```javascript
// User is somewhere, wants to go to level 3
fetch('/api/RouteNode/navigateToLevel', {
  method: 'POST',
  body: JSON.stringify({
    currentPosition: { latitude: 21.4225, longitude: 39.8262 },
    targetLevel: 3,
    floorId: 1
  })
});
// → Routes through level transitions (stairs/elevators) to reach level 3
```

## Performance Considerations

1. **Node Organization**: Nodes are grouped by level for O(1) level lookups
2. **Spatial Optimization**: Only visible nodes on the specified floor are considered
3. **Early Termination**: A* stops as soon as any node on the target level is reached
4. **Safety Limits**: Maximum iterations = nodes.Count * 10 to prevent infinite loops

## Requirements

### Database Setup
Ensure your route nodes have the `level` column populated:
- Level 1: Ground floor nodes
- Level 2: Second floor nodes
- Level 3: Third floor nodes
- etc.

### Level Connectors
For cross-level navigation to work, you must have nodes that connect different levels:
- Stairway nodes: Create connected nodes at the top and bottom of stairs
- Elevator nodes: Create connected nodes at each elevator stop
- Ensure these connector nodes have different `level` values but are connected via `connected_node_ids`

### Example Data Setup
```sql
-- Stairway: Level 1 to Level 2
INSERT INTO route_nodes (floor_id, level, geometry, connected_node_ids) 
VALUES (1, 1, ST_SetSRID(ST_MakePoint(39.8262, 21.4225), 4326), ARRAY[102]);

INSERT INTO route_nodes (floor_id, level, geometry, connected_node_ids) 
VALUES (1, 2, ST_SetSRID(ST_MakePoint(39.8262, 21.4225), 4326), ARRAY[101, 103]);

-- Level 2 to Level 3
INSERT INTO route_nodes (floor_id, level, geometry, connected_node_ids) 
VALUES (1, 2, ST_SetSRID(ST_MakePoint(39.8263, 21.4226), 4326), ARRAY[102, 104]);

INSERT INTO route_nodes (floor_id, level, geometry, connected_node_ids) 
VALUES (1, 3, ST_SetSRID(ST_MakePoint(39.8263, 21.4226), 4326), ARRAY[103]);
```

## Testing

### Test Case 1: User at Random Position (No Level)
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "targetLevel": 2,
  "floorId": 1
}
```
**Expected:** Path from closest node (any level) to a node on level 2

### Test Case 2: Already at Target Level
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "targetLevel": 1,
  "floorId": 1
}
```
**Expected:** If closest node is on level 1, returns single node or minimal path

### Test Case 3: Single Level Transition
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "targetLevel": 2,
  "floorId": 1
}
```
**Expected:** Path with at least one level transition marker (if start node is not on level 2)

### Test Case 4: Multiple Level Transitions
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "targetLevel": 3,
  "floorId": 1
}
```
**Expected:** Path traversing through intermediate levels to reach level 3

## Notes

- **No level required for start position**: The user's current position doesn't need to have a level assigned
- **Works for any routing scenario**:
  - Same-level routing (node to node on same level)
  - Cross-level routing (from any position to target level)
  - Start node can have no level, be on any level
- The algorithm assumes bidirectional connections between nodes
- Level transitions should be marked by having adjacent nodes with different `level` values
- The `floorId` parameter is optional but recommended for performance
- Pathfinding is limited to a single floor (or all floors if `floorId` is not specified)
- The algorithm will always find the shortest path if one exists
- Nodes without level information are handled gracefully (with a heuristic penalty)
