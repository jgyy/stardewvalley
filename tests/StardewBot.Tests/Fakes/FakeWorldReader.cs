using StardewBot.GameState;
using System.Collections.Generic;

namespace StardewBot.Tests.Fakes;

public class FakeWorldReader : IWorldReader
{
    public float EnergyPercent { get; set; } = 1.0f;
    public float InventoryFillRatio { get; set; } = 0.0f;
    public int MineFloorReached { get; set; } = 0;
    public IReadOnlyList<(float X, float Y)> CropsToWater { get; set; } = new List<(float X, float Y)>();
    public IReadOnlyList<(float X, float Y)> CropsToHarvest { get; set; } = new List<(float X, float Y)>();
    public IReadOnlyList<(float X, float Y)> ForagablePositions { get; set; } = new List<(float X, float Y)>();
    public IReadOnlyList<NpcInfo> Npcs { get; set; } = new List<NpcInfo>();
    public bool IsRaining { get; set; } = false;
}
