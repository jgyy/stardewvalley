using Microsoft.Xna.Framework;
using StardewBot.Executor;
using StardewBot.GameState;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using StardewValley.Tools;
using System;
using System.Linq;
using GameSeason = StardewBot.GameState.Season;

namespace StardewBot.Scoring;

public class FishAction : IAction
{
    public string Name => "Fish";

    private static readonly Point DefaultFishingSpot = new(57, 55);

    private Point   _fishingSpot;
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
        _fishingSpot = DefaultFishingSpot;
        _stuckTicks  = 0;
        _lastTile    = Vector2.Zero;
        _gaveUp      = false;
    }

    public bool Tick()
    {
        if (_gaveUp) return true;

        if (!LocationNavigator.NavigateTo("Forest")) return false;

        var playerTile = Game1.player.Tile;

        if (playerTile == _fishingSpot.ToVector2())
        {
            _stuckTicks = 0;
            CastRod();
            return false;
        }

        if (Game1.player.controller == null)
        {
            if (playerTile == _lastTile)
            {
                _stuckTicks++;
                if (_stuckTicks >= 60)
                {
                    var fallback = FindNearbyWaterSpot(Game1.currentLocation, playerTile);
                    if (fallback.HasValue)
                    {
                        _fishingSpot = fallback.Value;
                        _stuckTicks  = 0;
                    }
                    else
                    {
                        _gaveUp = true;
                        return true;
                    }
                }
            }
            else
            {
                _stuckTicks = 0;
                _lastTile   = playerTile;
            }

            Game1.player.controller = new PathFindController(
                Game1.player, Game1.currentLocation, _fishingSpot, 0
            );
        }

        return false;
    }

    private static void CastRod()
    {
        if (Game1.player.UsingTool || Game1.activeClickableMenu is BobberBar) return;
        var rod = Game1.player.Items.OfType<FishingRod>().FirstOrDefault();
        if (rod == null) return;
        Game1.player.CurrentTool = rod;
        Game1.player.BeginUsingTool();
    }

    private static Point? FindNearbyWaterSpot(GameLocation location, Vector2 origin)
    {
        int cx = (int)origin.X;
        int cy = (int)origin.Y;

        for (int radius = 1; radius <= 20; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                int x = cx + dx, y = cy + dy;
                if (!location.isWaterTile(x, y)) continue;

                var adjacent = new Point[] {
                    new(x - 1, y), new(x + 1, y), new(x, y - 1), new(x, y + 1)
                };
                foreach (var pt in adjacent)
                {
                    var pv = new Vector2(pt.X, pt.Y);
                    if (!location.Objects.ContainsKey(pv))
                        return pt;
                }
            }
        }
        return null;
    }
}
