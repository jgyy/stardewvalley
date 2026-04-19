using StardewBot.GameState;
using StardewValley;

namespace StardewBot.Scoring;

public class SleepAction : IAction
{
    public string Name => "Sleep";

    private bool _done;

    public float Score(DayContext ctx, IWorldReader world) => 999f;

    public void Begin(DayContext ctx, IWorldReader world) => _done = false;

    public bool Tick()
    {
        if (_done) return true;

        var farmhouse = Game1.getLocationFromName("FarmHouse");
        var bedTile = new Microsoft.Xna.Framework.Point(21, 4);

        if (Game1.currentLocation.Name != "FarmHouse")
        {
            Game1.player.controller = new StardewValley.Pathfinding.PathFindController(
                Game1.player, Game1.currentLocation,
                new Microsoft.Xna.Framework.Point(64, 15), 0,
                (c, loc) => Game1.warpFarmer("FarmHouse", 21, 4, false)
            );
            return false;
        }

        if (Game1.player.Tile != bedTile.ToVector2())
        {
            Game1.player.controller = new StardewValley.Pathfinding.PathFindController(
                Game1.player, farmhouse, bedTile, 0
            );
            return false;
        }

        Game1.NewDay(0.01f);
        _done = true;
        return true;
    }
}
