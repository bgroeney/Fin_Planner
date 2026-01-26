#!/bin/bash

# Ports to check
PORTS=(5173 5217)

for PORT in "${PORTS[@]}"; do
    # Find PIDs for the given port
    PIDS=$(lsof -t -i:"$PORT")
    
    if [ -n "$PIDS" ]; then
        echo "Found process(es) on port $PORT: $PIDS. Killing..."
        # Kill all PIDs found for this port
        echo "$PIDS" | xargs kill -9
    else
        echo "No processes found on port $PORT."
    fi
done
