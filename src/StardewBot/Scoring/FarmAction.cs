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
        _tilesToProcess = new Queue<Vector2>();
        foreach (var tile in world.CropsToHarvest) _tilesToProcess.Enqueue(tile);
        foreach (var tile in world.CropsToWater)   _tilesToProcess.Enqueue(tile);
        foreach (var tile in world.DebrisToClear)  _tilesToProcess.Enqueue(tile);
        _stuckTicks = 0;
    }

    public bool Tick()
    {
        if (_tilesToProcess == null || _tilesToProcess.Count == 0) return true;

        if (!LocationNavigator.NavigateTo("Farm")) return false;

        var tile = _tilesToProcess.Peek();
        var farm = Game1.getFarm();
        var playerTile = Game1.player.Tile;

        if (playerTile == _lastTile)
        {
            _stuckTicks++;
            if (_stuckTicks >= 4)
            {
                _tilesToProcess.Dequeue();
                _stuckTicks = 0;
                Game1.player.controller = null;
                return _tilesToProcess.Count == 0;
            }
        }
        else
        {
            _stuckTicks = 0;
            _lastTile   = playerTile;
        }

        if (IsAdjacentTo(playerTile, tile))
        {
            UseTool(farm, tile);
            _tilesToProcess.Dequeue();
            _stuckTicks = 0;
            Game1.player.controller = null;
            return _tilesToProcess.Count == 0;
        }

        if (Game1.player.controller == null)
            Game1.player.controller = new PathFindController(Game1.player, farm, tile.ToPoint(), 0);

        return false;
    }

    private static bool IsAdjacentTo(Vector2 player, Vector2 target)
    {
        int dx = Math.Abs((int)player.X - (int)target.X);
        int dy = Math.Abs((int)player.Y - (int)target.Y);
        return dx + dy <= 1;
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

        if (farm.Objects.TryGetValue(tile, out var obj))
        {
            Tool? tool = obj.Name switch
            {
                "Stone" => Game1.player.Items.OfType<Pickaxe>().FirstOrDefault(),
                "Twig"  => Game1.player.Items.OfType<Axe>().FirstOrDefault(),
                "Log"   => Game1.player.Items.OfType<Axe>().FirstOrDefault(),
                "Weeds" => Game1.player.Items.OfType<MeleeWeapon>().FirstOrDefault() as Tool
                        ?? Game1.player.Items.OfType<Axe>().FirstOrDefault(),
                _       => null,
            };

            if (tool != null)
            {
                var saved = Game1.player.CurrentTool;
                Game1.player.CurrentTool = tool;
                tool.DoFunction(farm, (int)tile.X * 64, (int)tile.Y * 64, 0, Game1.player);
                Game1.player.CurrentTool = saved;
            }
        }
    }
}
