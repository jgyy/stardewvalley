using StardewBot.GameState;

namespace StardewBot.Scoring;

public interface IAction
{
    string Name { get; }

    float Score(DayContext ctx, IWorldReader world);

    void Begin(DayContext ctx, IWorldReader world);

    bool Tick();
}
