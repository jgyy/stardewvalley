using Microsoft.Xna.Framework;
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

    private static readonly Point FishingSpot = new(57, 55);

    private Vector2 _lastTile;
    private int     _stuckTicks;
    private bool    _gaveUp;

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
        _stuckTicks = 0;
        _gaveUp     = false;
    }

    public bool Tick()
    {
        if (_gaveUp) return true;

        if (!LocationNavigator.NavigateTo("Forest")) return false;

        var playerTile = Game1.player.Tile;

        if (playerTile == _lastTile)
        {
            _stuckTicks++;
            if (_stuckTicks >= 3)
            {
                _gaveUp = true;
                Game1.player.controller = null;
                return true;
            }
        }
        else
        {
            _stuckTicks = 0;
            _lastTile   = playerTile;
        }

        if (playerTile != FishingSpot.ToVector2())
        {
            if (Game1.player.controller == null)
                Game1.player.controller = new PathFindController(
                    Game1.player, Game1.currentLocation, FishingSpot, 0
                );
            return false;
        }

        _stuckTicks = 0;

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
