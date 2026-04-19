using StardewBot.GameState;
using StardewBot.Scoring;
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
}
