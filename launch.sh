#!/usr/bin/env bash
set -e

GAME_DIR="$HOME/.local/share/Steam/steamapps/common/Stardew Valley"
REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
SMAPI_DLL="$GAME_DIR/StardewModdingAPI.dll"

if [ ! -f "$SMAPI_DLL" ] || [ "$(stat -c%s "$SMAPI_DLL")" -lt 100000 ]; then
    echo "ERROR: SMAPI not found or not properly installed."
    echo ""
    echo "Install it:"
    echo "  1. Download from https://smapi.io"
    echo "  2. Extract the zip"
    echo "  3. Run: bash 'install on Linux.sh'"
    exit 1
fi

echo "[StardewBot] Building mod..."
dotnet build "$REPO_DIR/src/StardewBot/" --configuration Debug --nologo -v quiet

echo "[StardewBot] Launching via SMAPI..."
cd "$GAME_DIR"
exec dotnet StardewModdingAPI.dll
