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
    private bool            _waitingForAnimation;

    private const float WorkRadius = 38f;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.EnergyPercent < 0.2f) return 0f;

        return new ScoreContext()
            .AddIf(world.CropsToHarvest.Count > 0, 40f)
            .AddIf(world.CropsToWater.Count > 0, 30f)
            .AddIf(world.DebrisToClear.Count > 0, 20f)
            .AddIf(world.ForagablePositions.Count > 0, 10f)
            .AddIf(world.EnergyPercent > 0.5f, 5f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        var farm = Game1.getFarm();

        var sortOrigin = Game1.player.Tile;
        if (!Game1.currentLocation.Name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
        {
            var house = farm.buildings.FirstOrDefault(b =>
                b.GetIndoorsName().Equals("FarmHouse", StringComparison.OrdinalIgnoreCase));
            sortOrigin = house != null
                ? new Vector2(house.tileX.Value + house.humanDoor.X, house.tileY.Value + house.humanDoor.Y)
                : new Vector2(64, 15);
        }

        var grassTiles = new List<Vector2>();
        var treeTiles  = new List<Vector2>();
        foreach (var pair in farm.terrainFeatures.Pairs)
        {
            if (ManhattanDist(pair.Key, sortOrigin) > WorkRadius) continue;
            if (pair.Value is Grass)
                grassTiles.Add(pair.Key);
            else if (pair.Value is Tree t && !t.tapped.Value)
                treeTiles.Add(pair.Key);
        }

        var allTiles = world.CropsToHarvest
            .Concat(world.CropsToWater)
            .Concat(world.DebrisToClear.Where(t => ManhattanDist(t, sortOrigin) <= WorkRadius))
            .Concat(world.ForagablePositions)
            .Concat(grassTiles)
            .Concat(treeTiles)
            .ToList();

        _tilesToProcess      = new Queue<Vector2>(NearestNeighborSort(allTiles, sortOrigin));
        _stuckTicks          = 0;
        _lastTile            = Vector2.Zero;
        _waitingForAnimation = false;
    }

    private static float ManhattanDist(Vector2 a, Vector2 b) =>
        Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

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

        var farm      = Game1.getFarm();
        var playerPos = Game1.player.Tile;

        if (_waitingForAnimation)
        {
            if (Game1.player.UsingTool) return false;
            _waitingForAnimation = false;
            var current = _tilesToProcess.Peek();
            // Trees need multiple hits — keep in queue until the terrain feature is gone
            bool treeStillStanding = farm.terrainFeatures.TryGetValue(current, out var tf) && tf is Tree;
            if (!treeStillStanding)
            {
                _tilesToProcess.Dequeue();
                Game1.player.controller = null;
                _stuckTicks = 0;
                return _tilesToProcess.Count == 0;
            }
            Game1.player.controller = null;
            return false;
        }

        var tile = _tilesToProcess.Peek();

        bool hasTerrain = farm.terrainFeatures.TryGetValue(tile, out var feature);
        bool hasObject  = farm.Objects.TryGetValue(tile, out var tileObj);

        if (!hasTerrain && !hasObject)
        {
            _tilesToProcess.Dequeue();
            Game1.player.controller = null;
            _stuckTicks = 0;
            return _tilesToProcess.Count == 0;
        }

        bool isBlockingTerrain = feature is Tree;
        bool isBlocking        = (hasObject && IsBlockingDebris(tileObj!)) || isBlockingTerrain;
        Point pathTarget = isBlocking
            ? GetAdjacentApproach(farm, tile) ?? tile.ToPoint()
            : tile.ToPoint();
        bool atTarget = isBlocking
            ? IsAdjacentTo(playerPos, tile)
            : playerPos == tile;

        if (atTarget)
        {
            bool needsWait = PerformAction(farm, tile, feature, tileObj, playerPos);
            if (needsWait)
            {
                _waitingForAnimation = true;
                return false;
            }
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

    private static bool PerformAction(Farm farm, Vector2 tile, TerrainFeature? feature, StardewValley.Object? obj, Vector2 playerPos)
    {
        if (feature is HoeDirt dirt)
        {
            if (dirt.readyForHarvest())
                dirt.crop?.harvest((int)tile.X, (int)tile.Y, dirt);
            else if (dirt.state.Value == HoeDirt.dry)
                dirt.state.Value = HoeDirt.watered;
            return false;
        }

        if (feature is Grass grass)
        {
            var scythe = Game1.player.Items.OfType<MeleeWeapon>().FirstOrDefault(m => m.isScythe());
            if (scythe != null)
            {
                Game1.player.faceDirection(FaceToward(playerPos, tile));
                Game1.player.CurrentTool = scythe;
                Game1.player.BeginUsingTool();
                grass.performToolAction(scythe, 1, tile);
                farm.terrainFeatures.Remove(tile);
            }
            return true;
        }

        if (feature is Tree)
        {
            var axe = Game1.player.Items.OfType<Axe>().FirstOrDefault();
            if (axe != null)
            {
                Game1.player.faceDirection(FaceToward(playerPos, tile));
                Game1.player.CurrentTool = axe;
                Game1.player.BeginUsingTool();
            }
            return true;
        }

        if (obj == null) return false;

        if (obj.isForage())
        {
            farm.checkAction(
                new xTile.Dimensions.Location((int)tile.X, (int)tile.Y),
                Game1.viewport, Game1.player);
            return false;
        }

        Tool? tool = obj.Name switch
        {
            "Stone" => Game1.player.Items.OfType<Pickaxe>().FirstOrDefault() as Tool,
            "Twig"  => Game1.player.Items.OfType<Axe>().FirstOrDefault() as Tool,
            "Log"   => Game1.player.Items.OfType<Axe>().FirstOrDefault() as Tool,
            "Weeds" => Game1.player.Items.OfType<MeleeWeapon>().FirstOrDefault(m => m.isScythe()) as Tool
                    ?? Game1.player.Items.OfType<Axe>().FirstOrDefault() as Tool,
            _       => null,
        };

        if (tool == null) return false;

        Game1.player.faceDirection(FaceToward(playerPos, tile));
        Game1.player.CurrentTool = tool;
        Game1.player.BeginUsingTool();
        return true;
    }

    private static int FaceToward(Vector2 from, Vector2 to)
    {
        int dx = (int)(to.X - from.X);
        int dy = (int)(to.Y - from.Y);
        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx > 0 ? 1 : 3;
        return dy > 0 ? 2 : 0;
    }

    private static bool IsBlockingDebris(StardewValley.Object obj) =>
        obj.Name is "Stone" or "Twig" or "Weeds" or "Log";

    private static bool IsAdjacentTo(Vector2 player, Vector2 target)
    {
        int dx = Math.Abs((int)player.X - (int)target.X);
        int dy = Math.Abs((int)player.Y - (int)target.Y);
        return dx + dy <= 1;
    }

    private static Point? GetAdjacentApproach(Farm farm, Vector2 tile)
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
            var pv = new Vector2(p.X, p.Y);
            if (!farm.Objects.ContainsKey(pv) && !farm.terrainFeatures.ContainsKey(pv))
                return p;
        }
        return null;
    }
}
