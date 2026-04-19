using StardewBot.GameState;
using StardewBot.Scoring;
using System.Collections.Generic;
using System.Linq;

namespace StardewBot.Executor;

public class ActionExecutor
{
    private readonly FishingMinigameHandler _fishHandler = new();
    private Queue<IAction>? _queue;
    private IAction? _current;
    private DayContext? _ctx;
    private IWorldReader? _world;

    public void StartDay(IReadOnlyList<IAction> queue, DayContext ctx, IWorldReader world)
    {
        _queue = new Queue<IAction>(queue);
        _ctx = ctx;
        _world = world;
        _current = null;
    }

    public bool Tick()
    {
        if (_ctx == null || _world == null) return false;

        bool energyLow = _world.EnergyPercent < 0.2f;
        bool almostNight = StardewValley.Game1.timeOfDay >= 2200;

        if ((energyLow || almostNight) && _current is not SleepAction)
        {
            var sleep = new SleepAction();
            sleep.Begin(_ctx, _world);
            _current = sleep;
        }

        if (_world.InventoryFillRatio > 0.95f && _current is not ShipAction && _current is not SleepAction)
        {
            var ship = new ShipAction();
            ship.Begin(_ctx, _world);
            var remaining = _queue != null ? _queue.ToArray() : System.Array.Empty<IAction>();
            _queue = new Queue<IAction>(new[] { ship }.Concat(remaining));
            _current = ship;
        }

        if (_fishHandler.IsActive)
        {
            _fishHandler.Tick();
            return true;
        }

        if (_current != null)
        {
            bool done = _current.Tick();
            if (!done) return true;
            _current = null;
        }

        if (_queue == null || _queue.Count == 0) return false;

        _current = _queue.Dequeue();
        _current.Begin(_ctx, _world);
        return true;
    }
}
