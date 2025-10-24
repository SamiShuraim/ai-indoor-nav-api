#!/bin/bash

# Test script for the adaptive load balancer
# This script simulates various scenarios to verify the implementation

BASE_URL="http://localhost:5000/api/LoadBalancer"

echo "=========================================="
echo "Testing Adaptive Load Balancer"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test 1: Check health endpoint
echo -e "${BLUE}Test 1: Health Check${NC}"
response=$(curl -s -X GET "${BASE_URL}/health")
echo "Response: $response"
echo ""

# Test 2: Get initial metrics
echo -e "${BLUE}Test 2: Get Initial Metrics${NC}"
response=$(curl -s -X GET "${BASE_URL}/metrics")
echo "Response: $response"
echo ""

# Test 3: Get initial config
echo -e "${BLUE}Test 3: Get Initial Configuration${NC}"
response=$(curl -s -X GET "${BASE_URL}/config")
echo "Response: $response"
echo ""

# Test 4: Assign a disabled pilgrim (should always go to Level 1)
echo -e "${BLUE}Test 4: Assign Disabled Pilgrim (Age 68)${NC}"
response=$(curl -s -X POST "${BASE_URL}/arrivals/assign" \
  -H "Content-Type: application/json" \
  -d '{"age": 68, "isDisabled": true}')
echo "Response: $response"
level=$(echo "$response" | grep -o '"level":[0-9]*' | grep -o '[0-9]*')
if [ "$level" = "1" ]; then
  echo -e "${GREEN}✓ PASS: Disabled pilgrim assigned to Level 1${NC}"
else
  echo -e "${RED}✗ FAIL: Expected Level 1, got Level $level${NC}"
fi
echo ""

# Test 5: Assign multiple non-disabled pilgrims with varying ages
echo -e "${BLUE}Test 5: Assign Multiple Non-Disabled Pilgrims${NC}"

ages=(25 35 45 55 65 75 85)
for age in "${ages[@]}"; do
  echo "  Assigning pilgrim aged $age..."
  response=$(curl -s -X POST "${BASE_URL}/arrivals/assign" \
    -H "Content-Type: application/json" \
    -d "{\"age\": $age, \"isDisabled\": false}")
  level=$(echo "$response" | grep -o '"level":[0-9]*' | grep -o '[0-9]*')
  echo "    -> Assigned to Level $level"
done
echo ""

# Test 6: Update level states
echo -e "${BLUE}Test 6: Update Level States${NC}"
response=$(curl -s -X POST "${BASE_URL}/levels/state" \
  -H "Content-Type: application/json" \
  -d '{
    "levels": [
      {"level": 1, "waitEst": 13.1, "queueLen": 120, "throughputPerMin": 10.5},
      {"level": 2, "waitEst": 15.2, "queueLen": 230, "throughputPerMin": 18.0},
      {"level": 3, "waitEst": 14.7, "queueLen": 210, "throughputPerMin": 17.2}
    ]
  }')
echo "Response: $response"
echo ""

# Test 7: Trigger controller tick
echo -e "${BLUE}Test 7: Trigger Controller Tick${NC}"
response=$(curl -s -X POST "${BASE_URL}/control/tick")
echo "Response: $response"
echo ""

# Test 8: Get updated metrics
echo -e "${BLUE}Test 8: Get Updated Metrics After Assignments${NC}"
response=$(curl -s -X GET "${BASE_URL}/metrics")
echo "Response: $response"
echo ""

# Test 9: Update configuration
echo -e "${BLUE}Test 9: Update Configuration${NC}"
response=$(curl -s -X POST "${BASE_URL}/config" \
  -H "Content-Type: application/json" \
  -d '{
    "alpha1": 0.4,
    "waitTargetMinutes": 10
  }')
echo "Response: $response"
echo ""

# Test 10: Verify config was updated
echo -e "${BLUE}Test 10: Verify Configuration Update${NC}"
response=$(curl -s -X GET "${BASE_URL}/config")
echo "Response: $response"
echo ""

# Test 11: Test cold start scenario (young non-disabled)
echo -e "${BLUE}Test 11: Young Non-Disabled Pilgrim${NC}"
response=$(curl -s -X POST "${BASE_URL}/arrivals/assign" \
  -H "Content-Type: application/json" \
  -d '{"age": 20, "isDisabled": false}')
echo "Response: $response"
level=$(echo "$response" | grep -o '"level":[0-9]*' | grep -o '[0-9]*')
if [ "$level" = "2" ] || [ "$level" = "3" ]; then
  echo -e "${GREEN}✓ PASS: Young pilgrim assigned to Level 2 or 3${NC}"
else
  echo -e "${RED}✗ FAIL: Expected Level 2 or 3, got Level $level${NC}"
fi
echo ""

# Test 12: Test invalid age (negative)
echo -e "${BLUE}Test 12: Test Invalid Age (Negative)${NC}"
response=$(curl -s -X POST "${BASE_URL}/arrivals/assign" \
  -H "Content-Type: application/json" \
  -d '{"age": -5, "isDisabled": false}')
echo "Response: $response"
if echo "$response" | grep -q "error"; then
  echo -e "${GREEN}✓ PASS: Negative age rejected with error${NC}"
else
  echo -e "${RED}✗ FAIL: Expected error for negative age${NC}"
fi
echo ""

# Test 13: Test invalid age (too high)
echo -e "${BLUE}Test 13: Test Invalid Age (Over 120)${NC}"
response=$(curl -s -X POST "${BASE_URL}/arrivals/assign" \
  -H "Content-Type: application/json" \
  -d '{"age": 150, "isDisabled": false}')
echo "Response: $response"
if echo "$response" | grep -q "error"; then
  echo -e "${GREEN}✓ PASS: Age over 120 rejected with error${NC}"
else
  echo -e "${RED}✗ FAIL: Expected error for age over 120${NC}"
fi
echo ""

# Test 14: Simulate batch of disabled pilgrims
echo -e "${BLUE}Test 14: Batch of Disabled Pilgrims${NC}"
echo "Assigning 10 disabled pilgrims..."
for i in {1..10}; do
  age=$((50 + RANDOM % 30))
  curl -s -X POST "${BASE_URL}/arrivals/assign" \
    -H "Content-Type: application/json" \
    -d "{\"age\": $age, \"isDisabled\": true}" > /dev/null
done
echo "Done. Checking metrics..."
response=$(curl -s -X GET "${BASE_URL}/metrics")
echo "Response: $response"
echo ""

# Test 15: Test legacy endpoint compatibility
echo -e "${BLUE}Test 15: Legacy Endpoint (/assign)${NC}"
response=$(curl -s -X POST "${BASE_URL}/assign" \
  -H "Content-Type: application/json" \
  -d '{"age": 60, "isHealthy": true}')
echo "Response: $response"
echo ""

echo "=========================================="
echo "Testing Complete"
echo "=========================================="
