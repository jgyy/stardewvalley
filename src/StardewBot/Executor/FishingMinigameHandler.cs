using System.Reflection;
using StardewValley;
using StardewValley.Menus;

namespace StardewBot.Executor;

public class FishingMinigameHandler
{
    private static readonly FieldInfo? BobberPositionField =
        typeof(BobberBar).GetField("bobberPosition", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? BarPosField =
        typeof(BobberBar).GetField("bobberBarPos", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo? BarHeightField =
        typeof(BobberBar).GetField("bobberBarHeight", BindingFlags.NonPublic | BindingFlags.Instance);

    public bool IsActive => Game1.activeClickableMenu is BobberBar;

    public bool Tick()
    {
        if (Game1.activeClickableMenu is not BobberBar bar) return false;

        float bobberPos = (float)(BobberPositionField?.GetValue(bar) ?? 0f);
        float barPos = (float)(BarPosField?.GetValue(bar) ?? 0f);
        float barHeight = (float)(BarHeightField?.GetValue(bar) ?? 100f);

        bool shouldHold = bobberPos < barPos + barHeight / 2f;

        Game1.oldMouseState = new Microsoft.Xna.Framework.Input.MouseState(
            0, 0, 0,
            shouldHold ? Microsoft.Xna.Framework.Input.ButtonState.Pressed
                       : Microsoft.Xna.Framework.Input.ButtonState.Released,
            Microsoft.Xna.Framework.Input.ButtonState.Released,
            Microsoft.Xna.Framework.Input.ButtonState.Released,
            Microsoft.Xna.Framework.Input.ButtonState.Released,
            Microsoft.Xna.Framework.Input.ButtonState.Released
        );

        return true;
    }
}
