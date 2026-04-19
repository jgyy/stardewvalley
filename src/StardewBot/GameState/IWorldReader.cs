using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace StardewBot.GameState;

public interface IWorldReader
{
    float EnergyPercent { get; }
    float InventoryFillRatio { get; }
    int MineFloorReached { get; }
    IReadOnlyList<Vector2> CropsToWater { get; }
    IReadOnlyList<Vector2> CropsToHarvest { get; }
    IReadOnlyList<Vector2> ForagablePositions { get; }
    IReadOnlyList<NpcInfo> Npcs { get; }
    bool IsRaining { get; }
}
