using System.Collections.Generic;

namespace StardewBot.GameState;

public interface IWorldReader
{
    float EnergyPercent { get; }
    float InventoryFillRatio { get; }
    int MineFloorReached { get; }
    IReadOnlyList<(float X, float Y)> CropsToWater { get; }
    IReadOnlyList<(float X, float Y)> CropsToHarvest { get; }
    IReadOnlyList<(float X, float Y)> ForagablePositions { get; }
    IReadOnlyList<NpcInfo> Npcs { get; }
    bool IsRaining { get; }
}
