using Microsoft.Xna.Framework;
using StardewBot.GameState;
using System.Collections.Generic;

namespace StardewBot.Tests.Fakes;

public class FakeWorldReader : IWorldReader
{
    public float EnergyPercent { get; set; } = 1.0f;
    public float InventoryFillRatio { get; set; } = 0.0f;
    public int MineFloorReached { get; set; } = 0;
    public IReadOnlyList<Vector2> CropsToWater { get; set; } = new List<Vector2>();
    public IReadOnlyList<Vector2> CropsToHarvest { get; set; } = new List<Vector2>();
    public IReadOnlyList<Vector2> ForagablePositions { get; set; } = new List<Vector2>();
    public IReadOnlyList<NpcInfo> Npcs { get; set; } = new List<NpcInfo>();
    public bool IsRaining { get; set; } = false;
}
