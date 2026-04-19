using StardewBot.GameState;
using StardewValley;
using StardewValley.Tools;
using System.Linq;
using GameSeason = StardewBot.GameState.Season;

namespace StardewBot.Scoring;

public class FishAction : IAction
{
    public string Name => "Fish";

    private bool _casting;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (ctx.Season == GameSeason.Winter) return 0f;
        if (world.EnergyPercent < 0.3f) return 0f;

        return new ScoreContext()
            .Add(15f)
            .AddIf(world.IsRaining, 20f)
            .AddIf(ctx.Season == GameSeason.Spring || ctx.Season == GameSeason.Fall, 10f)
            .AddIf(world.InventoryFillRatio < 0.5f, 5f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _casting = false;
    }

    public bool Tick()
    {
        var fishingSpot = new Microsoft.Xna.Framework.Point(57, 55);

        if (Game1.currentLocation.Name != "Forest")
        {
            Game1.warpFarmer("Forest", fishingSpot.X, fishingSpot.Y, false);
            return false;
        }

        if (!_casting)
        {
            if (Game1.player.Tile != fishingSpot.ToVector2())
            {
                var forest = Game1.currentLocation;
                Game1.player.controller = new StardewValley.Pathfinding.PathFindController(
                    Game1.player, forest, fishingSpot, 0
                );
                return false;
            }

            var rod = Game1.player.Items.OfType<FishingRod>().FirstOrDefault();
            if (rod == null) return true;
            Game1.player.CurrentTool = rod;
            Game1.player.BeginUsingTool();
            _casting = true;
            return false;
        }

        return false;
    }
}
