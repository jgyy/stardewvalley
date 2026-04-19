namespace StardewBot.GameState;

public enum Season { Spring, Summer, Fall, Winter }
public enum Weather { Sunny, Rainy, Windy, Snowy }

public record DayContext(Season Season, int Day, int TimeOfDay, Weather Weather = Weather.Sunny)
{
    public bool IsLateNight => TimeOfDay >= 2400;
    public bool IsAlmostNight => TimeOfDay >= 2200;
}
