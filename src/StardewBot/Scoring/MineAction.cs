using System;
using System.Linq;
using StardewBot.Executor;
using StardewBot.GameState;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace StardewBot.Scoring;

public class MineAction : IAction
{
    public string Name => "Mine";

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

    public void Begin(DayContext ctx, IWorldReader world) { }

    public bool Tick()
    {
        if (Game1.currentLocation is MineShaft) return false;

        if (!LocationNavigator.NavigateTo("Mountain")) return false;

        var mineWarp = Game1.currentLocation.warps.FirstOrDefault(w =>
            w.TargetName.StartsWith("UndergroundMine", StringComparison.OrdinalIgnoreCase) ||
            w.TargetName.Equals("Mine", StringComparison.OrdinalIgnoreCase));

        if (mineWarp == null) return false;

        if (Game1.player.controller == null)
        {
            Game1.player.controller = new PathFindController(
                Game1.player,
                Game1.currentLocation,
                new Microsoft.Xna.Framework.Point(mineWarp.X, mineWarp.Y),
                0
            );
        }
        return false;
    }
}
