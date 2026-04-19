using StardewBot.GameState;

namespace StardewBot.Scoring;

public interface IAction
{
    string Name { get; }

    // Returns score 0-100+ (higher = higher priority today)
    float Score(DayContext ctx, IWorldReader world);

    // Called once when action is selected; capture state here
    void Begin(DayContext ctx, IWorldReader world);

    // Called each UpdateTicked; return true when the action is complete
    bool Tick();
}
