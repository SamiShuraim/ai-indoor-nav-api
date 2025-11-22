# Level-to-Level Navigation API

## Overview
This endpoint enables cross-level navigation for indoor positioning. After a user's position is trilaterated and they are assigned a level, this API provides a route to navigate from their current level to a target level.

## Endpoint

### Navigate to Level
**POST** `/api/RouteNode/navigateToLevel`

Finds the shortest path from the user's current position to a target level, handling level transitions (stairs, elevators, etc.).

#### Request Body
```json
{
  "currentPosition": {
    "latitude": 21.4225,
    "longitude": 39.8262
  },
  "currentLevel": 1,
  "targetLevel": 3,
  "floorId": 1
}
```

#### Request Parameters
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `currentPosition` | `LocationPoint` | Yes | User's current GPS coordinates |
| `currentPosition.latitude` | `double` | Yes | Latitude coordinate |
| `currentPosition.longitude` | `double` | Yes | Longitude coordinate |
| `currentLevel` | `int` | Yes | Current level (1, 2, 3, etc.) |
| `targetLevel` | `int` | Yes | Target level to navigate to |
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
  "error": "Current position, current level, and target level are required."
}
```

**404 Not Found**
```json
{
  "error": "No route nodes found on floor 1 at level 1."
}
```
or
```json
{
  "error": "No path found from level 1 to level 3."
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

1. **Find Start Node**: Locates the nearest node to the user's current position on their current level
2. **Cross-Level Pathfinding**: Uses A* to find the shortest path considering:
   - Physical distance between nodes
   - Level transition costs
   - Level difference heuristic
3. **Path Construction**: Returns the complete path with all nodes and edges
4. **Transition Marking**: Identifies and marks level transitions (stairs/elevators)

## Usage Example

### Mobile App Integration

```javascript
// After trilateration and level assignment
const currentPosition = {
  latitude: 21.4225,
  longitude: 39.8262
};

const currentLevel = 1; // Assigned by load balancer
const targetLevel = 3;  // User wants to go to level 3
const floorId = 1;

// Make API request
const response = await fetch('https://your-api.com/api/RouteNode/navigateToLevel', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    currentPosition,
    currentLevel,
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

### Test Case 1: Same Level
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "currentLevel": 2,
  "targetLevel": 2,
  "floorId": 1
}
```
**Expected:** Empty FeatureCollection (already at target level)

### Test Case 2: Single Level Transition
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "currentLevel": 1,
  "targetLevel": 2,
  "floorId": 1
}
```
**Expected:** Path with at least one level transition marker

### Test Case 3: Multiple Level Transitions
**Request:**
```json
{
  "currentPosition": {"latitude": 21.4225, "longitude": 39.8262},
  "currentLevel": 1,
  "targetLevel": 3,
  "floorId": 1
}
```
**Expected:** Path traversing through level 2 to reach level 3

## Notes

- The algorithm assumes bidirectional connections between nodes
- Level transitions should be marked by having adjacent nodes with different `level` values
- The `floorId` parameter is optional but recommended for performance
- Pathfinding is limited to a single floor (or all floors if `floorId` is not specified)
- The algorithm will always find the shortest path if one exists
