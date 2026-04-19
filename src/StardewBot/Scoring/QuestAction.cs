using Microsoft.Xna.Framework;
using StardewBot.Executor;
using StardewBot.GameState;
using StardewValley;
using StardewValley.Pathfinding;
using StardewValley.Quests;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewBot.Scoring;

public class QuestAction : IAction
{
    public string Name => "Quest";

    private Queue<(string npcName, string mapName)>? _deliveries;
    private int _ticksOnCurrent;
    private const int MaxTicksPerDelivery = 400;

    private static readonly HashSet<string> _navMaps = new(StringComparer.OrdinalIgnoreCase)
        { "Farm", "FarmHouse", "BusStop", "Town", "Mountain", "Beach", "Forest" };

    public float Score(DayContext ctx, IWorldReader world)
    {
        return GetDeliverableQuests().Any() ? 60f : 0f;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _deliveries     = new Queue<(string, string)>();
        _ticksOnCurrent = 0;

        foreach (var quest in GetDeliverableQuests())
        {
            var npc = Utility.fuzzyCharacterSearch(quest.target.Value);
            if (npc == null) continue;
            _deliveries.Enqueue((quest.target.Value, ResolveNavMap(npc.currentLocation?.Name ?? "Town")));
        }
    }

    public bool Tick()
    {
        if (_deliveries == null || _deliveries.Count == 0) return true;

        var (npcName, mapName) = _deliveries.Peek();
        _ticksOnCurrent++;

        if (_ticksOnCurrent > MaxTicksPerDelivery)
        {
            _deliveries.Dequeue();
            _ticksOnCurrent = 0;
            Game1.player.controller = null;
            return _deliveries.Count == 0;
        }

        if (!LocationNavigator.NavigateTo(mapName)) return false;

        var npc = Utility.fuzzyCharacterSearch(npcName);
        if (npc == null)
        {
            _deliveries.Dequeue();
            _ticksOnCurrent = 0;
            return _deliveries.Count == 0;
        }

        var playerPos = Game1.player.Tile;
        if (IsAdjacentTo(playerPos, npc.Tile))
        {
            Game1.currentLocation.checkAction(
                new xTile.Dimensions.Location((int)npc.Tile.X, (int)npc.Tile.Y),
                Game1.viewport, Game1.player);
            _deliveries.Dequeue();
            _ticksOnCurrent = 0;
            Game1.player.controller = null;
            return _deliveries.Count == 0;
        }

        if (Game1.player.controller == null)
            Game1.player.controller = new PathFindController(
                Game1.player, Game1.currentLocation, npc.Tile.ToPoint(), 0);

        return false;
    }

    private static IEnumerable<ItemDeliveryQuest> GetDeliverableQuests() =>
        Game1.player.questLog
            .OfType<ItemDeliveryQuest>()
            .Where(q => !q.completed.Value &&
                Game1.player.Items.Any(i => i?.ItemId == q.ItemId.Value));

    private static string ResolveNavMap(string locationName) =>
        _navMaps.Contains(locationName) ? locationName : "Town";

    private static bool IsAdjacentTo(Vector2 player, Vector2 target)
    {
        int dx = Math.Abs((int)player.X - (int)target.X);
        int dy = Math.Abs((int)player.Y - (int)target.Y);
        return dx + dy <= 1;
    }
}
