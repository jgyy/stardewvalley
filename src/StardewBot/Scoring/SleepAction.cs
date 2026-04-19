using StardewBot.GameState;
using StardewValley;

namespace StardewBot.Scoring;

public class SleepAction : IAction
{
    public string Name => "Sleep";

    private bool _done;
    private bool _warping;

    public float Score(DayContext ctx, IWorldReader world) => 999f;

    public void Begin(DayContext ctx, IWorldReader world) { _done = false; _warping = false; }

    public bool Tick()
    {
        if (_done) return true;

        if (Game1.currentLocation.Name != "FarmHouse")
        {
            if (!_warping) { Game1.warpFarmer("FarmHouse", 5, 9, false); _warping = true; }
            return false;
        }
        _warping = false;

        Game1.NewDay(0.01f);
        _done = true;
        return true;
    }
}
