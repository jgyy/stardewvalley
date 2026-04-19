using StardewBot.GameState;
using StardewBot.Scoring;
using StardewBot.Tests.Fakes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Xunit;

namespace StardewBot.Tests;

public class ScoringTests
{
    [Fact]
    public void DayContext_Spring_Day1_IsFirstDayOfSpring()
    {
        var ctx = new DayContext(Season.Spring, Day: 1, TimeOfDay: 600);
        Assert.Equal(Season.Spring, ctx.Season);
        Assert.Equal(1, ctx.Day);
        Assert.Equal(600, ctx.TimeOfDay);
    }

    [Fact]
    public void ScoreContext_AddsModifiersCorrectly()
    {
        var score = new ScoreContext()
            .Add(30f)
            .AddIf(true, 20f)
            .AddIf(false, 50f);

        Assert.Equal(50f, score.Total);
    }

    [Fact]
    public void FarmAction_ScoresHighWhenCropsNeedHarvesting()
    {
        var world = new FakeWorldReader
        {
            CropsToHarvest = new List<Vector2> { new(1, 1) },
            EnergyPercent = 0.8f
        };
        var ctx = new DayContext(Season.Spring, 5, 800);
        var action = new FarmAction();

        float score = action.Score(ctx, world);

        Assert.True(score >= 40f, $"Expected >= 40, got {score}");
    }

    [Fact]
    public void FarmAction_ScoresLowWhenNoCropsAndNoWatering()
    {
        var world = new FakeWorldReader { EnergyPercent = 0.8f };
        var ctx = new DayContext(Season.Spring, 5, 800);
        var action = new FarmAction();

        float score = action.Score(ctx, world);

        Assert.True(score < 10f, $"Expected < 10, got {score}");
    }

    [Fact]
    public void FarmAction_ScoresZeroWhenEnergyTooLow()
    {
        var world = new FakeWorldReader
        {
            CropsToHarvest = new List<Vector2> { new(1, 1) },
            EnergyPercent = 0.1f
        };
        var ctx = new DayContext(Season.Spring, 5, 800);
        var action = new FarmAction();

        float score = action.Score(ctx, world);

        Assert.Equal(0f, score);
    }

    [Fact]
    public void MineAction_ScoresHighMidSeason()
    {
        var world = new FakeWorldReader { InventoryFillRatio = 0.3f, EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Spring, 8, 800);
        var action = new MineAction();

        float score = action.Score(ctx, world);

        Assert.True(score >= 30f, $"Expected >= 30, got {score}");
    }

    [Fact]
    public void MineAction_ScoresLowWhenInventoryFull()
    {
        var world = new FakeWorldReader { InventoryFillRatio = 0.95f, EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Spring, 8, 800);
        var action = new MineAction();

        float score = action.Score(ctx, world);

        Assert.True(score < 10f, $"Expected < 10, got {score}");
    }

    [Fact]
    public void FishAction_ScoresHigherOnRainyDay()
    {
        var worldRainy = new FakeWorldReader { IsRaining = true, EnergyPercent = 0.9f };
        var worldSunny = new FakeWorldReader { IsRaining = false, EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Summer, 10, 800);
        var action = new FishAction();

        float rainyScore = action.Score(ctx, worldRainy);
        float sunnyScore = action.Score(ctx, worldSunny);

        Assert.True(rainyScore > sunnyScore, "Rainy should score higher than sunny");
    }

    [Fact]
    public void FishAction_ScoresZeroInWinter()
    {
        var world = new FakeWorldReader { EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Winter, 10, 800, Weather.Rainy);
        var action = new FishAction();

        float score = action.Score(ctx, world);

        Assert.Equal(0f, score);
    }

    [Fact]
    public void ForageAction_ScoresHigherInSpringAndFall()
    {
        var world = new FakeWorldReader
        {
            ForagablePositions = new List<Vector2> { new(5, 5) },
            EnergyPercent = 0.9f
        };
        var springCtx = new DayContext(Season.Spring, 5, 800);
        var summerCtx = new DayContext(Season.Summer, 5, 800);
        var action = new ForageAction();

        float springScore = action.Score(springCtx, world);
        float summerScore = action.Score(summerCtx, world);

        Assert.True(springScore > summerScore);
    }

    [Fact]
    public void ForageAction_ScoresZeroWhenNoForagables()
    {
        var world = new FakeWorldReader { EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Spring, 5, 800);
        var action = new ForageAction();

        Assert.Equal(0f, action.Score(ctx, world));
    }
}
