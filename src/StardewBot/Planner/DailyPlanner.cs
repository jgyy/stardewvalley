using StardewBot.GameState;
using StardewBot.Scoring;
using System.Collections.Generic;
using System.Linq;

namespace StardewBot.Planner;

public class DailyPlanner
{
    private readonly IReadOnlyList<IAction> _actions;

    public DailyPlanner(IEnumerable<IAction> actions)
    {
        _actions = actions.ToList();
    }

    public IReadOnlyList<IAction> BuildQueue(DayContext ctx, IWorldReader world)
    {
        return _actions
            .Select(a => (Action: a, Score: a.Score(ctx, world)))
            .Where(x => x.Score > 0f)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Action)
            .ToList();
    }
}
