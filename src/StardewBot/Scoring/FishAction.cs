using StardewBot.Executor;
using StardewBot.GameState;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using StardewValley.Tools;
using System.Linq;
using GameSeason = StardewBot.GameState.Season;

namespace StardewBot.Scoring;

public class FishAction : IAction
{
    public string Name => "Fish";

    private static readonly Microsoft.Xna.Framework.Point FishingSpot = new(57, 55);

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

    public void Begin(DayContext ctx, IWorldReader world) { }

    public bool Tick()
    {
        if (!LocationNavigator.NavigateTo("Forest")) return false;

        if (Game1.player.Tile != FishingSpot.ToVector2())
        {
            if (Game1.player.controller == null)
                Game1.player.controller = new PathFindController(
                    Game1.player, Game1.currentLocation, FishingSpot, 0
                );
            return false;
        }

        if (!Game1.player.UsingTool && Game1.activeClickableMenu is not BobberBar)
        {
            var rod = Game1.player.Items.OfType<FishingRod>().FirstOrDefault();
            if (rod == null) return true;
            Game1.player.CurrentTool = rod;
            Game1.player.BeginUsingTool();
        }
        return false;
    }
}
