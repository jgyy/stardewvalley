using StardewBot.Executor;
using StardewBot.Planner;
using StardewBot.Scoring;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Linq;
using GameSeason = StardewBot.GameState.Season;
using DayCtx = StardewBot.GameState.DayContext;
using WeatherEnum = StardewBot.GameState.Weather;

namespace StardewBot;

public class ModEntry : Mod
{
    private DailyPlanner? _planner;
    private ActionExecutor? _executor;
    private readonly StardewBot.GameState.WorldReader _world = new();

    public override void Entry(IModHelper helper)
    {
        _planner = new DailyPlanner(new IAction[]
        {
            new QuestAction(),
            new FarmAction(),
            new MineAction(),
            new FishAction(),
            new ForageAction(),
            new SocialAction(),
            new ShipAction(),
        });
        _executor = new ActionExecutor();

        LocationNavigator.Init(Monitor);
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        var ctx = BuildContext();
        var queue = _planner!.BuildQueue(ctx, _world);
        _executor!.StartDay(queue, ctx, _world);
        Monitor.Log($"[StardewBot] Day {ctx.Day} {ctx.Season} — queue: {string.Join(", ", queue.Select(a => a.Name))}", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady) return;

        if (Game1.activeClickableMenu is DialogueBox db)
        {
            if (e.IsMultipleOf(30))
                db.receiveLeftClick(Game1.viewport.Width / 2, Game1.viewport.Height / 2);
            return;
        }

        if (!Context.IsPlayerFree) return;
        _executor?.Tick();
    }

    private DayCtx BuildContext()
    {
        var season = Game1.currentSeason switch
        {
            "spring" => GameSeason.Spring,
            "summer" => GameSeason.Summer,
            "fall"   => GameSeason.Fall,
            _        => GameSeason.Winter
        };
        var weather = Game1.isRaining ? WeatherEnum.Rainy
                    : Game1.isSnowing ? WeatherEnum.Snowy
                    : WeatherEnum.Sunny;
        return new DayCtx(season, Game1.dayOfMonth, Game1.timeOfDay, weather);
    }
}
