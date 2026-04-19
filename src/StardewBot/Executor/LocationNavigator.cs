using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewBot.Executor;

public static class LocationNavigator
{
    private static readonly Dictionary<string, string[]> _graph = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FarmHouse"] = new[] { "Farm" },
        ["Farm"]      = new[] { "FarmHouse", "BusStop", "Forest" },
        ["BusStop"]   = new[] { "Farm", "Town" },
        ["Town"]      = new[] { "BusStop", "Mountain", "Beach", "Forest" },
        ["Mountain"]  = new[] { "Town" },
        ["Forest"]    = new[] { "Farm", "Town" },
        ["Beach"]     = new[] { "Town" },
    };

    private static IMonitor? _monitor;
    private static string?   _warpingTo;
    private static string?   _lastLocation;
    private static Vector2   _lastPlayerTile;
    private static int       _stuckTicks;

    private const int StepSize = 12;

    public static void Init(IMonitor monitor) => _monitor = monitor;

    public static bool NavigateTo(string targetLocation)
    {
        var current = Game1.currentLocation;

        if (_lastLocation != null && !current.Name.Equals(_lastLocation, StringComparison.OrdinalIgnoreCase))
        {
            Game1.player.controller = null;
            _stuckTicks = 0;
            _monitor?.Log($"[Nav] Map changed {_lastLocation}→{current.Name}: state reset", LogLevel.Debug);
        }
        _lastLocation = current.Name;

        if (_warpingTo != null && current.Name.Equals(_warpingTo, StringComparison.OrdinalIgnoreCase))
            _warpingTo = null;

        if (current.Name.Equals(targetLocation, StringComparison.OrdinalIgnoreCase))
            return true;

        if (_warpingTo != null) return false;

        var nextMap = FindNextHop(current.Name, targetLocation);
        if (nextMap == null)
        {
            _monitor?.Log($"[Nav] No route from {current.Name} to {targetLocation}", LogLevel.Warn);
            return false;
        }

        var warp = current.warps.FirstOrDefault(w =>
            w.TargetName.Equals(nextMap, StringComparison.OrdinalIgnoreCase));

        if (warp == null && nextMap.Equals("FarmHouse", StringComparison.OrdinalIgnoreCase) && current.Name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
        {
            var farm  = Game1.getFarm();
            var house = farm.buildings.FirstOrDefault(b =>
                b.GetIndoorsName().Equals("FarmHouse", StringComparison.OrdinalIgnoreCase));
            if (house == null)
            {
                _monitor?.Log($"[Nav] FarmHouse building not found on Farm", LogLevel.Warn);
                return false;
            }

            var doorTile  = new Point(house.tileX.Value + house.humanDoor.X, house.tileY.Value + house.humanDoor.Y);
            var playerPos = Game1.player.Tile;
            int dx = Math.Abs((int)playerPos.X - doorTile.X);
            int dy = Math.Abs((int)playerPos.Y - doorTile.Y);

            if (dx + dy <= 2)
            {
                _monitor?.Log($"[Nav] Triggering warpFarmer → FarmHouse (5,9)", LogLevel.Info);
                _stuckTicks = 0;
                _warpingTo  = "FarmHouse";
                Game1.warpFarmer("FarmHouse", 5, 9, false);
                return false;
            }

            if (Game1.player.controller == null)
            {
                if (playerPos == _lastPlayerTile)
                {
                    _stuckTicks++;
                    _monitor?.Log($"[Nav] Stuck tick {_stuckTicks} at ({(int)playerPos.X},{(int)playerPos.Y})", LogLevel.Debug);
                }
                else
                {
                    _stuckTicks     = 0;
                    _lastPlayerTile = playerPos;
                }

                Point pathTarget;
                if (_stuckTicks >= 5)
                {
                    pathTarget  = StepToward(playerPos, doorTile);
                    _stuckTicks = 0;
                    _monitor?.Log($"[Nav] Stepping toward FarmHouse door: player=({(int)playerPos.X},{(int)playerPos.Y}) step=({pathTarget.X},{pathTarget.Y}) door=({doorTile.X},{doorTile.Y})", LogLevel.Debug);
                }
                else
                {
                    var approachTile = GetApproachTile(current, doorTile);
                    pathTarget = approachTile;
                    _monitor?.Log($"[Nav] Farm→FarmHouse: door=({doorTile.X},{doorTile.Y}) player=({(int)playerPos.X},{(int)playerPos.Y}) dist={dx + dy} → approach=({approachTile.X},{approachTile.Y})", LogLevel.Debug);
                }

                Game1.player.controller = new PathFindController(Game1.player, current, pathTarget, -1);
            }

            return false;
        }

        if (warp == null)
        {
            _monitor?.Log($"[Nav] No warp to '{nextMap}' in {current.Name}. Available: [{string.Join(", ", current.warps.Select(w2 => $"{w2.TargetName}@({w2.X},{w2.Y})"))}]", LogLevel.Warn);
            return false;
        }

        var warpTile  = new Point(warp.X, warp.Y);
        var playerPos2 = Game1.player.Tile;
        int dx2 = Math.Abs((int)playerPos2.X - warpTile.X);
        int dy2 = Math.Abs((int)playerPos2.Y - warpTile.Y);

        if (dx2 + dy2 <= 2)
        {
            _monitor?.Log($"[Nav] Triggering warpFarmer → {warp.TargetName} ({warp.TargetX},{warp.TargetY})", LogLevel.Info);
            _stuckTicks = 0;
            _warpingTo  = warp.TargetName;
            Game1.warpFarmer(warp.TargetName, warp.TargetX, warp.TargetY, false);
            return false;
        }

        if (Game1.player.controller == null)
        {
            if (playerPos2 == _lastPlayerTile)
            {
                _stuckTicks++;
                _monitor?.Log($"[Nav] Stuck tick {_stuckTicks} at ({(int)playerPos2.X},{(int)playerPos2.Y})", LogLevel.Debug);
            }
            else
            {
                _stuckTicks     = 0;
                _lastPlayerTile = playerPos2;
            }

            Point pathTarget;

            if (_stuckTicks >= 5)
            {
                pathTarget = StepToward(playerPos2, warpTile);
                _stuckTicks = 0;
                _monitor?.Log($"[Nav] Stepping toward warp: player=({(int)playerPos2.X},{(int)playerPos2.Y}) step=({pathTarget.X},{pathTarget.Y}) warp=({warpTile.X},{warpTile.Y})", LogLevel.Debug);
            }
            else
            {
                var approachTile = GetApproachTile(current, warpTile);
                pathTarget = approachTile;
                _monitor?.Log($"[Nav] {current.Name}→{nextMap}: warp=({warpTile.X},{warpTile.Y}) player=({(int)playerPos2.X},{(int)playerPos2.Y}) dist={dx2 + dy2} → approach=({approachTile.X},{approachTile.Y})", LogLevel.Debug);
            }

            Game1.player.controller = new PathFindController(Game1.player, current, pathTarget, -1);
        }

        return false;
    }

    private static Point StepToward(Vector2 from, Point to)
    {
        float diffX = to.X - from.X;
        float diffY = to.Y - from.Y;
        float dist  = MathF.Sqrt(diffX * diffX + diffY * diffY);

        if (dist <= StepSize)
            return to;

        float scale = StepSize / dist;
        return new Point((int)(from.X + diffX * scale), (int)(from.Y + diffY * scale));
    }

    private static Point GetApproachTile(GameLocation location, Point warpTile)
    {
        var layer = location.Map.GetLayer("Back");
        int mapW = layer?.LayerWidth  ?? 9999;
        int mapH = layer?.LayerHeight ?? 9999;

        if (warpTile.Y >= mapH - 2) return new Point(warpTile.X, Math.Max(0, warpTile.Y - 2));
        if (warpTile.Y <= 1)        return new Point(warpTile.X, Math.Min(mapH - 1, warpTile.Y + 2));
        if (warpTile.X >= mapW - 2) return new Point(Math.Max(0, warpTile.X - 2), warpTile.Y);
        if (warpTile.X <= 1)        return new Point(Math.Min(mapW - 1, warpTile.X + 2), warpTile.Y);
        return warpTile;
    }

    private static string? FindNextHop(string from, string to)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { from };
        var queue   = new Queue<(string node, string firstHop)>();

        if (!_graph.TryGetValue(from, out var neighbors)) return null;
        foreach (var n in neighbors) queue.Enqueue((n, n));

        while (queue.Count > 0)
        {
            var (node, hop) = queue.Dequeue();
            if (node.Equals(to, StringComparison.OrdinalIgnoreCase)) return hop;
            if (!visited.Add(node)) continue;
            if (_graph.TryGetValue(node, out var next))
                foreach (var n in next)
                    if (!visited.Contains(n))
                        queue.Enqueue((n, hop));
        }
        return null;
    }
}
