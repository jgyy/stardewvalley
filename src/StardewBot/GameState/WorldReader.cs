using Microsoft.Xna.Framework;
using StardewBot.GameState;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;

namespace StardewBot.GameState;

public class WorldReader : IWorldReader
{
    public float EnergyPercent =>
        Game1.player.MaxStamina > 0
            ? Game1.player.stamina / (float)Game1.player.MaxStamina
            : 0f;

    public float InventoryFillRatio =>
        Game1.player.MaxItems > 0
            ? (float)Game1.player.Items.Count(i => i != null) / Game1.player.MaxItems
            : 0f;

    public int MineFloorReached => MineShaft.lowestLevelReached;

    public bool IsRaining => Game1.isRaining;

    public IReadOnlyList<Vector2> CropsToWater
    {
        get
        {
            var result = new List<Vector2>();
            foreach (var pair in Game1.getFarm().terrainFeatures.Pairs)
                if (pair.Value is HoeDirt { state.Value: HoeDirt.dry } dirt && dirt.crop != null)
                    result.Add(pair.Key);
            return result;
        }
    }

    public IReadOnlyList<Vector2> CropsToHarvest
    {
        get
        {
            var result = new List<Vector2>();
            foreach (var pair in Game1.getFarm().terrainFeatures.Pairs)
                if (pair.Value is HoeDirt dirt && dirt.readyForHarvest())
                    result.Add(pair.Key);
            return result;
        }
    }

    public IReadOnlyList<Vector2> ForagablePositions
    {
        get
        {
            var result = new List<Vector2>();
            foreach (var obj in Game1.currentLocation.Objects.Pairs)
                if (obj.Value.isForage()) result.Add(obj.Key);
            return result;
        }
    }

    public IReadOnlyList<NpcInfo> Npcs
    {
        get
        {
            var result = new List<NpcInfo>();
            foreach (var npc in Utility.getAllCharacters().OfType<NPC>())
            {
                if (!npc.IsVillager) continue;
                int hearts = Game1.player.getFriendshipHeartLevelForNPC(npc.Name);
                bool hasGift = Game1.player.Items.Any(item =>
                    item != null && npc.getGiftTasteForThisItem(item) is 0 or 2
                );
                result.Add(new NpcInfo(npc.Name, hearts, npc.isBirthday(), hasGift));
            }
            return result;
        }
    }
}
