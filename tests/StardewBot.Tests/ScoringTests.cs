using StardewBot.GameState;
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
}
