using StardewBot.Executor;
using StardewBot.GameState;
using StardewValley;

namespace StardewBot.Scoring;

public class SleepAction : IAction
{
    public string Name => "Sleep";

    private bool _done;

    public float Score(DayContext ctx, IWorldReader world) => 999f;

    public void Begin(DayContext ctx, IWorldReader world) { _done = false; }

    public bool Tick()
    {
        if (_done) return true;

        if (!LocationNavigator.NavigateTo("FarmHouse")) return false;

        Game1.NewDay(0.01f);
        _done = true;
        return true;
    }
}
