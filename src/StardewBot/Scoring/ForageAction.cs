using Microsoft.Xna.Framework;
using StardewBot.GameState;
using StardewValley;
using System.Collections.Generic;
using GameSeason = StardewBot.GameState.Season;

namespace StardewBot.Scoring;

public class ForageAction : IAction
{
    public string Name => "Forage";

    private Queue<Vector2>? _targets;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.ForagablePositions.Count == 0) return 0f;
        if (world.EnergyPercent < 0.2f) return 0f;

        return new ScoreContext()
            .Add(10f)
            .AddIf(ctx.Season == GameSeason.Spring || ctx.Season == GameSeason.Fall, 15f)
            .AddIf(world.ForagablePositions.Count > 3, 10f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _targets = new Queue<Vector2>(world.ForagablePositions);
    }

    public bool Tick()
    {
        if (_targets == null || _targets.Count == 0) return true;

        var tile = _targets.Peek();
        if (Game1.player.Tile == tile)
        {
            Game1.currentLocation.checkAction(
                new xTile.Dimensions.Location((int)tile.X, (int)tile.Y),
                Game1.viewport,
                Game1.player
            );
            _targets.Dequeue();
            return _targets.Count == 0;
        }

        Game1.player.controller = new StardewValley.Pathfinding.PathFindController(
            Game1.player, Game1.currentLocation, tile.ToPoint(), 0
        );
        return false;
    }
}
