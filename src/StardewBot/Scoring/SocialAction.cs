using StardewBot.GameState;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using GameSeason = StardewBot.GameState.Season;

namespace StardewBot.Scoring;

public class SocialAction : IAction
{
    public string Name => "Social";

    private List<NpcInfo>? _targets;

    public float Score(DayContext ctx, IWorldReader world)
    {
        var actionableNpcs = world.Npcs
            .Where(n => n.FriendshipHearts < 10)
            .ToList();

        if (actionableNpcs.Count == 0) return 0f;

        var score = new ScoreContext();
        foreach (var npc in actionableNpcs)
        {
            score.AddIf(npc.IsBirthday && npc.HasPreferredGiftAvailable, 50f);
            score.AddIf(!npc.IsBirthday && npc.HasPreferredGiftAvailable, 20f);
            score.AddIf(npc.FriendshipHearts < 4, 10f);
        }
        return score.Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _targets = world.Npcs
            .Where(n => n.FriendshipHearts < 10 && n.HasPreferredGiftAvailable)
            .ToList();
    }

    public bool Tick()
    {
        if (_targets == null || _targets.Count == 0) return true;

        var target = _targets[0];
        var npc = Game1.getCharacterFromName(target.Name);
        if (npc == null) { _targets.RemoveAt(0); return _targets.Count == 0; }

        if (Game1.player.Tile != npc.Tile)
        {
            Game1.player.controller = new StardewValley.Pathfinding.PathFindController(
                Game1.player, Game1.currentLocation, npc.TilePoint, 1
            );
            return false;
        }

        var activeObject = Game1.player.ActiveObject;
        if (activeObject != null)
            npc.receiveGift(activeObject, Game1.player, true, 1f, true);
        _targets.RemoveAt(0);
        return _targets.Count == 0;
    }
}
