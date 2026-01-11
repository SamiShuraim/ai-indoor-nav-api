#!/bin/bash

# =====================================================
# QUICK FIX SCRIPT FOR MISSING COLUMNS ERROR
# =====================================================
# This script helps you quickly fix the missing 
# connected_levels column error
# =====================================================

set -e  # Exit on error

echo "=========================================="
echo "  Missing Columns Quick Fix Script"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${GREEN}ℹ${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

# Check if .env file exists
if [ ! -f .env ]; then
    print_error ".env file not found!"
    print_info "Please create a .env file with your database connection string"
    print_info "You can copy .env.example and fill in your credentials:"
    echo ""
    echo "    cp .env.example .env"
    echo "    # Then edit .env and add your database password"
    echo ""
    exit 1
fi

# Load environment variables
print_info "Loading environment variables from .env..."
export $(cat .env | grep -v '^#' | xargs)

# Check if DEFAULT_CONNECTION is set
if [ -z "$DEFAULT_CONNECTION" ]; then
    print_error "DEFAULT_CONNECTION not found in .env file"
    exit 1
fi

print_success "Connection string loaded"

# Parse connection string
# Extract host, port, database, and username from connection string
HOST=$(echo $DEFAULT_CONNECTION | grep -oP 'Host=\K[^;]+')
PORT=$(echo $DEFAULT_CONNECTION | grep -oP 'Port=\K[^;]+')
DATABASE=$(echo $DEFAULT_CONNECTION | grep -oP 'Database=\K[^;]+')
USERNAME=$(echo $DEFAULT_CONNECTION | grep -oP 'Username=\K[^;]+')
PASSWORD=$(echo $DEFAULT_CONNECTION | grep -oP 'Password=\K[^;]+')

if [ -z "$PASSWORD" ] || [ "$PASSWORD" = "YOUR_SUPABASE_PASSWORD" ] || [ "$PASSWORD" = "[YOUR_PASSWORD]" ]; then
    print_error "Database password not set or still using placeholder"
    print_info "Please update your .env file with the actual database password"
    exit 1
fi

print_info "Database: $HOST:$PORT/$DATABASE"
print_info "Username: $USERNAME"
echo ""

# Check if psql is installed
if ! command -v psql &> /dev/null; then
    print_error "psql (PostgreSQL client) is not installed"
    print_info "Please install it:"
    echo ""
    echo "  Ubuntu/Debian:  sudo apt-get install postgresql-client"
    echo "  MacOS:          brew install postgresql"
    echo "  Windows:        Download from https://www.postgresql.org/download/windows/"
    echo ""
    print_info "OR run the SQL script manually using Supabase SQL Editor:"
    echo ""
    echo "  1. Go to https://app.supabase.com/"
    echo "  2. Select your project"
    echo "  3. Go to SQL Editor"
    echo "  4. Open and run: diagnose_and_fix_migration.sql"
    echo ""
    exit 1
fi

print_success "PostgreSQL client found"
echo ""

# Ask user what they want to do
print_info "What would you like to do?"
echo ""
echo "  1) Diagnose and fix the issue (RECOMMENDED)"
echo "  2) Just diagnose (check what's wrong)"
echo "  3) Apply migration only (if you know columns are missing)"
echo "  4) Exit"
echo ""
read -p "Choose an option (1-4): " choice

case $choice in
    1)
        print_info "Running diagnostic and fix script..."
        PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -U "$USERNAME" -d "$DATABASE" \
            -f diagnose_and_fix_migration.sql
        print_success "Fix completed! Check the output above for verification."
        ;;
    2)
        print_info "Running diagnostic checks only..."
        PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -U "$USERNAME" -d "$DATABASE" \
            -c "SELECT column_name FROM information_schema.columns WHERE table_name = 'route_nodes' AND column_name IN ('is_connection_point', 'connection_type', 'connected_levels', 'connection_priority');"
        ;;
    3)
        print_info "Applying migration script..."
        PGPASSWORD="$PASSWORD" psql -h "$HOST" -p "$PORT" -U "$USERNAME" -d "$DATABASE" \
            -f apply_connection_point_migration.sql
        print_success "Migration applied!"
        ;;
    4)
        print_info "Exiting..."
        exit 0
        ;;
    *)
        print_error "Invalid choice"
        exit 1
        ;;
esac

echo ""
print_success "Done! You can now test your API endpoint:"
echo ""
echo "  curl -X POST http://localhost:10000/api/RouteNode/navigateToLevel \\"
echo "    -H \"Content-Type: application/json\" \\"
echo "    -d '{\"currentNodeId\": 1, \"targetLevel\": 2}'"
echo ""
