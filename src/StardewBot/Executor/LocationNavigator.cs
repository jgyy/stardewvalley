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
    private static string? _warpingTo;
    private static string? _lastLocation;

    public static void Init(IMonitor monitor) => _monitor = monitor;

    public static bool NavigateTo(string targetLocation)
    {
        var current = Game1.currentLocation;

        if (_lastLocation != null && !current.Name.Equals(_lastLocation, StringComparison.OrdinalIgnoreCase))
        {
            _monitor?.Log($"[Nav] Map changed {_lastLocation}→{current.Name}: clearing stale controller", LogLevel.Debug);
            Game1.player.controller = null;
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

        if (warp == null)
        {
            _monitor?.Log($"[Nav] No warp to '{nextMap}' in {current.Name}. Warps: [{string.Join(", ", current.warps.Select(w2 => $"{w2.TargetName}@({w2.X},{w2.Y})"))}]", LogLevel.Warn);
            return false;
        }

        var warpTile = new Point(warp.X, warp.Y);

        if (Game1.player.controller != null) return false;

        var playerTile = Game1.player.Tile;
        int dx = Math.Abs((int)playerTile.X - warpTile.X);
        int dy = Math.Abs((int)playerTile.Y - warpTile.Y);

        _monitor?.Log($"[Nav] {current.Name}→{nextMap}: warp=({warpTile.X},{warpTile.Y}) player=({(int)playerTile.X},{(int)playerTile.Y}) dist={dx + dy}", LogLevel.Debug);

        if (dx + dy <= 2)
        {
            _monitor?.Log($"[Nav] Triggering warpFarmer → {warp.TargetName} ({warp.TargetX},{warp.TargetY})", LogLevel.Info);
            _warpingTo = warp.TargetName;
            Game1.warpFarmer(warp.TargetName, warp.TargetX, warp.TargetY, false);
            return false;
        }

        var approachTile = GetApproachTile(current, warpTile);
        _monitor?.Log($"[Nav] Pathfinding to approach=({approachTile.X},{approachTile.Y})", LogLevel.Debug);
        Game1.player.controller = new PathFindController(Game1.player, current, approachTile, -1);
        return false;
    }

    private static Point GetApproachTile(GameLocation location, Point warpTile)
    {
        var layer = location.Map.GetLayer("Back");
        int mapW = layer?.LayerWidth ?? 9999;
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
        var queue = new Queue<(string node, string firstHop)>();

        if (!_graph.TryGetValue(from, out var neighbors)) return null;
        foreach (var n in neighbors)
            queue.Enqueue((n, n));

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
