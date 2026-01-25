#!/bin/bash
# FinPlanner Local Development Setup
# Run this once to set up the local database

set -e

echo "=== FinPlanner Local Development Setup ==="

# Check for PostgreSQL
if ! command -v psql &> /dev/null; then
    echo "❌ PostgreSQL not found. Install with: brew install postgresql@15"
    exit 1
fi

# Start PostgreSQL if not running
if ! pg_isready -q 2>/dev/null; then
    echo "Starting PostgreSQL..."
    brew services start postgresql@15 2>/dev/null || brew services start postgresql 2>/dev/null || true
    sleep 2
fi

# Create database and user if they don't exist
echo "Setting up database..."
psql postgres -c "CREATE USER finplanner WITH PASSWORD 'devpassword';" 2>/dev/null || true
psql postgres -c "CREATE DATABASE \"FinPlanner\" OWNER finplanner;" 2>/dev/null || true
psql postgres -c "GRANT ALL PRIVILEGES ON DATABASE \"FinPlanner\" TO finplanner;" 2>/dev/null || true

echo "✅ Database ready: FinPlanner"

# Run migrations
echo "Running database migrations..."
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_DIR/Mineplex.FinPlanner.Api"
dotnet ef database update

echo ""
echo "=== Setup Complete ==="
echo ""
echo "To run the application:"
echo "  Terminal 1 (API):      cd Mineplex.FinPlanner.Api && dotnet run"
echo "  Terminal 2 (Frontend): cd fin-planner-ui && npm run dev"
echo ""
echo "Access:"
echo "  Frontend: http://localhost:5173"
echo "  API:      http://localhost:5217"
echo "  API Docs: http://localhost:5217/scalar/v1"
