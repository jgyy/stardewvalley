namespace StardewBot.Scoring;

public class ScoreContext
{
    private float _total;

    public ScoreContext Add(float value)
    {
        _total += value;
        return this;
    }

    public ScoreContext AddIf(bool condition, float value)
    {
        if (condition) _total += value;
        return this;
    }

    public float Total => _total;
}
