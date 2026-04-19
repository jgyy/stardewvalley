using Microsoft.Xna.Framework;
using StardewBot.Executor;
using StardewBot.GameState;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;

namespace StardewBot.Scoring;

public class FarmAction : IAction
{
    public string Name => "Farm";

    private Queue<Vector2>? _tilesToProcess;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.EnergyPercent < 0.2f) return 0f;

        return new ScoreContext()
            .AddIf(world.CropsToHarvest.Count > 0, 40f)
            .AddIf(world.CropsToWater.Count > 0, 30f)
            .AddIf(world.EnergyPercent > 0.5f, 5f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _tilesToProcess = new Queue<Vector2>();
        foreach (var tile in world.CropsToHarvest) _tilesToProcess.Enqueue(tile);
        foreach (var tile in world.CropsToWater) _tilesToProcess.Enqueue(tile);
    }

    public bool Tick()
    {
        if (_tilesToProcess == null || _tilesToProcess.Count == 0) return true;

        if (!LocationNavigator.NavigateTo("Farm")) return false;

        var tile = _tilesToProcess.Peek();
        var farm = Game1.getFarm();

        if (Game1.player.Tile == tile)
        {
            if (farm.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt)
            {
                if (dirt.readyForHarvest())
                    dirt.crop?.harvest((int)tile.X, (int)tile.Y, dirt);
                else if (dirt.state.Value == HoeDirt.dry)
                    dirt.state.Value = HoeDirt.watered;
            }
            _tilesToProcess.Dequeue();
            return _tilesToProcess.Count == 0;
        }

        if (Game1.player.controller == null)
            Game1.player.controller = new PathFindController(Game1.player, farm, tile.ToPoint(), 0);
        return false;
    }
}
