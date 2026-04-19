using StardewBot.GameState;
using StardewBot.Planner;
using StardewBot.Scoring;
using StardewBot.Tests.Fakes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Xunit;

namespace StardewBot.Tests;

public class PlannerTests
{
    [Fact]
    public void DailyPlanner_PutsHighestScoringActionFirst()
    {
        var world = new FakeWorldReader
        {
            CropsToHarvest = new List<Vector2> { new(1, 1) },
            EnergyPercent = 0.9f,
            InventoryFillRatio = 0.1f
        };
        var ctx = new DayContext(Season.Spring, 5, 800);
        var planner = new DailyPlanner(new IAction[]
        {
            new FarmAction(),
            new ShipAction(),
        });

        var queue = planner.BuildQueue(ctx, world);

        Assert.Equal("Farm", queue[0].Name);
    }

    [Fact]
    public void DailyPlanner_ExcludesActionsWithZeroScore()
    {
        var world = new FakeWorldReader { EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Winter, 5, 800);
        var planner = new DailyPlanner(new IAction[]
        {
            new FishAction(),
            new ForageAction()
        });

        var queue = planner.BuildQueue(ctx, world);

        Assert.Empty(queue);
    }
}
