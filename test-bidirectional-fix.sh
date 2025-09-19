#!/bin/bash

# Test script for the bidirectional connection fix API

echo "=== Testing Bidirectional Connection Fix API ==="
echo ""

# Test with floor ID 1 (based on the logs you provided)
FLOOR_ID=1
API_URL="http://localhost:5000/api/RouteNode/fixBidirectionalConnections"

echo "Sending request to fix bidirectional connections for floor $FLOOR_ID..."
echo "URL: $API_URL"
echo ""

# Make the API call
curl -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d "{\"floorId\": $FLOOR_ID}" \
  -w "\nHTTP Status: %{http_code}\n" \
  -s | jq '.' 2>/dev/null || echo "Response (raw JSON):"

echo ""
echo "=== Test completed ==="
echo ""
echo "Expected response format:"
echo '{'
echo '  "success": true,'
echo '  "floorId": 1,'
echo '  "fixedConnections": <number>,'
echo '  "report": "<detailed report>",'
echo '  "timestamp": "<ISO date>"'
echo '}'