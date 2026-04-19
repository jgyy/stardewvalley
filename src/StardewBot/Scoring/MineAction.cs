using System;
using StardewBot.GameState;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace StardewBot.Scoring;

public class MineAction : IAction
{
    public string Name => "Mine";

    private bool _started;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.EnergyPercent < 0.3f) return 0f;
        if (world.InventoryFillRatio > 0.85f) return 0f;

        bool isMidSeason = ctx.Day is >= 5 and <= 20;

        return new ScoreContext()
            .AddIf(isMidSeason, 30f)
            .AddIf(world.MineFloorReached < 40, 15f)
            .AddIf(world.EnergyPercent > 0.7f, 10f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _started = false;
    }

    public bool Tick()
    {
        if (!_started)
        {
            var mountain = Game1.getLocationFromName("Mountain");
            var mineEntrance = new Microsoft.Xna.Framework.Point(124, 100);
            Game1.player.controller = new PathFindController(
                Game1.player, mountain, mineEntrance, 0,
                (c, loc) => Game1.enterMine(Math.Min(120, MineShaft.lowestLevelReached + 1))
            );
            _started = true;
            return false;
        }

        return Game1.currentLocation is MineShaft mine &&
               mine.mineLevel >= MineShaft.lowestLevelReached;
    }
}
