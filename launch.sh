#!/usr/bin/env bash
set -e

GAME_DIR="$HOME/.local/share/Steam/steamapps/common/Stardew Valley"
REPO_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "[StardewBot] Building mod..."
dotnet build "$REPO_DIR/src/StardewBot/" --configuration Debug --nologo -v quiet

echo "[StardewBot] Launching via SMAPI..."
cd "$GAME_DIR"
exec dotnet StardewModdingAPI.dll
