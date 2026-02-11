#!/bin/bash

# RAT - Find the Gem!
# A fun puzzle game where you guide a rat through a dungeon to find gems

cd "$(dirname "$0")"

echo "=========================================="
echo "  RAT - Find the Gem!"
echo "=========================================="
echo ""
echo "Controls:"
echo "  WASD / Arrow Keys - Move"
echo "  ESC / Q - Quit"
echo ""
echo "Starting game..."
echo ""

dotnet run --project src/Rat.Desktop/Rat.Desktop.csproj
