using StardewBot.GameState;
using StardewValley;
using System.Linq;

namespace StardewBot.Scoring;

public class ShipAction : IAction
{
    public string Name => "Ship";

    private bool _done;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.InventoryFillRatio < 0.8f) return 0f;
        return new ScoreContext()
            .Add(60f)
            .AddIf(world.InventoryFillRatio > 0.95f, 20f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world) => _done = false;

    public bool Tick()
    {
        if (_done) return true;

        if (Game1.currentLocation.Name != "Farm")
        {
            Game1.warpFarmer("Farm", 64, 15, false);
            return false;
        }

        var farm = Game1.getFarm();
        var bin = farm.getBuildingByType("Shipping Bin");
        if (bin == null) { _done = true; return true; }

        var binTile = new Microsoft.Xna.Framework.Point(
            (int)bin.tileX.Value + 1, (int)bin.tileY.Value + 1
        );

        if (Game1.player.Tile != binTile.ToVector2())
        {
            Game1.player.controller = new StardewValley.Pathfinding.PathFindController(
                Game1.player, farm, binTile, 0
            );
            return false;
        }

        foreach (var item in Game1.player.Items.ToList())
        {
            if (item != null) farm.shipItem(item, Game1.player);
        }
        _done = true;
        return true;
    }
}
