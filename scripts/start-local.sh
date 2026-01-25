#!/bin/bash
# Start FinPlanner locally (no Docker required)
# Runs API and Frontend in background

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

echo "=== Starting FinPlanner ==="

# Check PostgreSQL
if ! pg_isready -q 2>/dev/null; then
    echo "Starting PostgreSQL..."
    brew services start postgresql@15 2>/dev/null || brew services start postgresql 2>/dev/null
    sleep 2
fi

# Start API in background
echo "Starting API..."
cd "$PROJECT_DIR/Mineplex.FinPlanner.Api"
dotnet run --urls "http://localhost:5217" &
API_PID=$!

# Wait for API to start
echo "Waiting for API to be ready..."
sleep 5

# Start Frontend
echo "Starting Frontend..."
cd "$PROJECT_DIR/fin-planner-ui"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "=== FinPlanner Running ==="
echo "Frontend: http://localhost:5173"
echo "API:      http://localhost:5217"
echo ""
echo "Press Ctrl+C to stop all services"

# Handle shutdown
trap "echo 'Stopping...'; kill $API_PID $FRONTEND_PID 2>/dev/null; exit" SIGINT SIGTERM

# Wait for processes
wait
