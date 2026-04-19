using Microsoft.Xna.Framework;
using StardewBot.Executor;
using StardewBot.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace StardewBot.Scoring;

public class FarmAction : IAction
{
    public string Name => "Farm";

    private Queue<Vector2>? _tilesToProcess;
    private Vector2         _lastTile;
    private int             _stuckTicks;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.EnergyPercent < 0.2f) return 0f;

        return new ScoreContext()
            .AddIf(world.CropsToHarvest.Count > 0, 40f)
            .AddIf(world.CropsToWater.Count > 0, 30f)
            .AddIf(world.DebrisToClear.Count > 0, 20f)
            .AddIf(world.EnergyPercent > 0.5f, 5f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        var allTiles = world.CropsToHarvest
            .Concat(world.CropsToWater)
            .Concat(world.DebrisToClear)
            .ToList();

        var sortOrigin = Game1.player.Tile;
        if (!Game1.currentLocation.Name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
        {
            var farm = Game1.getFarm();
            var house = farm.buildings.FirstOrDefault(b =>
                b.GetIndoorsName().Equals("FarmHouse", StringComparison.OrdinalIgnoreCase));
            sortOrigin = house != null
                ? new Vector2(house.tileX.Value + house.humanDoor.X, house.tileY.Value + house.humanDoor.Y)
                : new Vector2(64, 15);
        }

        _tilesToProcess = new Queue<Vector2>(NearestNeighborSort(allTiles, sortOrigin));
        _stuckTicks = 0;
        _lastTile   = Vector2.Zero;
    }

    private static IEnumerable<Vector2> NearestNeighborSort(List<Vector2> tiles, Vector2 start)
    {
        var remaining = new List<Vector2>(tiles);
        var current   = start;
        while (remaining.Count > 0)
        {
            int   bestIdx  = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                float d = Math.Abs(remaining[i].X - current.X) + Math.Abs(remaining[i].Y - current.Y);
                if (d < bestDist) { bestDist = d; bestIdx = i; }
            }
            current = remaining[bestIdx];
            yield return current;
            remaining.RemoveAt(bestIdx);
        }
    }

    public bool Tick()
    {
        if (_tilesToProcess == null || _tilesToProcess.Count == 0) return true;

        if (!LocationNavigator.NavigateTo("Farm")) return false;

        var tile      = _tilesToProcess.Peek();
        var farm      = Game1.getFarm();
        var playerPos = Game1.player.Tile;

        bool isCrop   = farm.terrainFeatures.ContainsKey(tile);
        bool isDebris = farm.Objects.ContainsKey(tile);
        if (!isCrop && !isDebris)
        {
            _tilesToProcess.Dequeue();
            Game1.player.controller = null;
            _stuckTicks = 0;
            return _tilesToProcess.Count == 0;
        }

        Point pathTarget = isDebris
            ? GetDebrisApproach(farm, tile) ?? tile.ToPoint()
            : tile.ToPoint();

        bool atTarget = isDebris
            ? IsAdjacentTo(playerPos, tile)
            : playerPos == tile;

        if (atTarget)
        {
            UseTool(farm, tile);
            _tilesToProcess.Dequeue();
            Game1.player.controller = null;
            _stuckTicks = 0;
            return _tilesToProcess.Count == 0;
        }

        if (Game1.player.controller == null)
        {
            if (playerPos == _lastTile)
            {
                _stuckTicks++;
                if (_stuckTicks >= 20)
                {
                    _tilesToProcess.Dequeue();
                    _stuckTicks = 0;
                    return _tilesToProcess.Count == 0;
                }
            }
            else
            {
                _stuckTicks = 0;
                _lastTile   = playerPos;
            }

            Game1.player.controller = new PathFindController(Game1.player, farm, pathTarget, 0);
        }

        return false;
    }

    private static bool IsAdjacentTo(Vector2 player, Vector2 target)
    {
        int dx = Math.Abs((int)player.X - (int)target.X);
        int dy = Math.Abs((int)player.Y - (int)target.Y);
        return dx + dy <= 1;
    }

    private static Point? GetDebrisApproach(Farm farm, Vector2 tile)
    {
        var candidates = new Point[]
        {
            new((int)tile.X,     (int)tile.Y - 1),
            new((int)tile.X,     (int)tile.Y + 1),
            new((int)tile.X - 1, (int)tile.Y    ),
            new((int)tile.X + 1, (int)tile.Y    ),
        };
        foreach (var p in candidates)
        {
            if (p.X < 0 || p.Y < 0) continue;
            var v = new Vector2(p.X, p.Y);
            if (!farm.Objects.ContainsKey(v))
                return p;
        }
        return null;
    }

    private static void UseTool(Farm farm, Vector2 tile)
    {
        if (farm.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt)
        {
            if (dirt.readyForHarvest())
                dirt.crop?.harvest((int)tile.X, (int)tile.Y, dirt);
            else if (dirt.state.Value == HoeDirt.dry)
                dirt.state.Value = HoeDirt.watered;
            return;
        }

        if (!farm.Objects.TryGetValue(tile, out var obj)) return;

        Tool? tool = obj.Name switch
        {
            "Stone" => Game1.player.Items.OfType<Pickaxe>().FirstOrDefault(),
            "Twig"  => Game1.player.Items.OfType<Axe>().FirstOrDefault(),
            "Log"   => Game1.player.Items.OfType<Axe>().FirstOrDefault(),
            "Weeds" => (Tool?)Game1.player.Items.OfType<MeleeWeapon>().FirstOrDefault()
                    ?? Game1.player.Items.OfType<Axe>().FirstOrDefault(),
            _       => null,
        };

        if (tool == null) return;
        var saved = Game1.player.CurrentTool;
        Game1.player.CurrentTool = tool;
        tool.DoFunction(farm, (int)tile.X * 64, (int)tile.Y * 64, 0, Game1.player);
        Game1.player.CurrentTool = saved;
    }
}
