using Microsoft.Xna.Framework;
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

    // Returns true when the player is at targetLocation.
    // Otherwise pathfinds to the nearest warp tile leading toward it.
    public static bool NavigateTo(string targetLocation)
    {
        var current = Game1.currentLocation;
        if (current.Name.Equals(targetLocation, StringComparison.OrdinalIgnoreCase))
            return true;

        var nextMap = FindNextHop(current.Name, targetLocation);
        if (nextMap == null) return false;

        var warp = current.warps.FirstOrDefault(w =>
            w.TargetName.Equals(nextMap, StringComparison.OrdinalIgnoreCase));
        if (warp == null) return false;

        var dest = new Point(warp.X, warp.Y);
        if (Game1.player.controller == null)
        {
            Game1.player.controller = new PathFindController(
                Game1.player, current, dest, 0
            );
        }
        return false;
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
