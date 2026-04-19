# Stardew Valley Bot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a SMAPI C# mod that fully auto-plays Stardew Valley indefinitely using a utility-AI scoring system.

**Architecture:** Four layers — GameState (reads world), Scoring (IAction implementations score themselves), Planner (builds daily queue), Executor (drives player movement per tick). IWorldReader interface decouples scoring logic from game DLLs for unit testing.

**Tech Stack:** C# / net6.0, SMAPI 4.x via `Pathoschild.Stardew.ModBuildConfig`, xUnit 2.x for scorer/planner unit tests.

---

## File Map

| File | Responsibility |
|---|---|
| `src/StardewBot/manifest.json` | SMAPI mod manifest |
| `src/StardewBot/StardewBot.csproj` | Mod project, references ModBuildConfig |
| `src/StardewBot/ModEntry.cs` | SMAPI entry point, wires all layers to events |
| `src/StardewBot/GameState/DayContext.cs` | Plain data: season, day, time — no game deps |
| `src/StardewBot/GameState/IWorldReader.cs` | Interface: crops, energy, NPC info, inventory |
| `src/StardewBot/GameState/WorldReader.cs` | Implements IWorldReader using Game1 statics |
| `src/StardewBot/Scoring/IAction.cs` | Interface: Score, Begin, Tick |
| `src/StardewBot/Scoring/ScoreContext.cs` | Additive score builder |
| `src/StardewBot/Scoring/FarmAction.cs` | Water/harvest crops |
| `src/StardewBot/Scoring/MineAction.cs` | Descend mines |
| `src/StardewBot/Scoring/FishAction.cs` | Fish at best location |
| `src/StardewBot/Scoring/ForageAction.cs` | Collect foragables |
| `src/StardewBot/Scoring/SocialAction.cs` | Gift/talk to NPCs |
| `src/StardewBot/Scoring/ShipAction.cs` | Ship items when bag full |
| `src/StardewBot/Scoring/SleepAction.cs` | Force sleep (energy guard) |
| `src/StardewBot/Planner/DailyPlanner.cs` | Runs all scorers, returns ordered queue |
| `src/StardewBot/Executor/ActionExecutor.cs` | Tick loop, energy/time guards, queue management |
| `src/StardewBot/Executor/FishingMinigameHandler.cs` | BobberBar PID controller |
| `tests/StardewBot.Tests/StardewBot.Tests.csproj` | Test project |
| `tests/StardewBot.Tests/Fakes/FakeWorldReader.cs` | IWorldReader stub for tests |
| `tests/StardewBot.Tests/ScoringTests.cs` | Tests for all IAction.Score() methods |
| `tests/StardewBot.Tests/PlannerTests.cs` | Tests for DailyPlanner ordering |

---

## Task 1: Project Scaffolding

**Files:**
- Create: `src/StardewBot/StardewBot.csproj`
- Create: `src/StardewBot/manifest.json`
- Create: `tests/StardewBot.Tests/StardewBot.Tests.csproj`

- [ ] **Step 1: Create the mod project file**

Create `src/StardewBot/StardewBot.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>10</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.*" />
  </ItemGroup>
</Project>
```

> `ModBuildConfig` auto-resolves Stardew Valley + SMAPI DLL references from your game install. No manual DLL referencing needed.

- [ ] **Step 2: Create the SMAPI manifest**

Create `src/StardewBot/manifest.json`:

```json
{
  "Name": "StardewBot",
  "Author": "jgyy",
  "Version": "1.0.0",
  "Description": "Utility-AI bot that auto-plays Stardew Valley indefinitely.",
  "UniqueID": "jgyy.StardewBot",
  "EntryDll": "StardewBot.dll",
  "MinimumApiVersion": "4.0.0"
}
```

- [ ] **Step 3: Create the test project file**

Create `tests/StardewBot.Tests/StardewBot.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>10</LangVersion>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <ProjectReference Include="../../src/StardewBot/StardewBot.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create a solution file and verify it builds**

```bash
cd /home/jegoh/Documents/repo/stardewvalley
dotnet new sln -n StardewBot
dotnet sln add src/StardewBot/StardewBot.csproj
dotnet sln add tests/StardewBot.Tests/StardewBot.Tests.csproj
dotnet build
```

Expected: build succeeds (may warn about missing ModEntry — that's fine).

- [ ] **Step 5: Commit**

```bash
git add src/ tests/ StardewBot.sln
git commit -m "feat: scaffold SMAPI mod and test projects"
```

---

## Task 2: DayContext

**Files:**
- Create: `src/StardewBot/GameState/DayContext.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/StardewBot.Tests/ScoringTests.cs` with a placeholder test to verify DayContext compiles:

```csharp
using StardewBot.GameState;
using Xunit;

namespace StardewBot.Tests;

public class ScoringTests
{
    [Fact]
    public void DayContext_Spring_Day1_IsFirstDayOfSpring()
    {
        var ctx = new DayContext(Season.Spring, day: 1, timeOfDay: 600);
        Assert.Equal(Season.Spring, ctx.Season);
        Assert.Equal(1, ctx.Day);
        Assert.Equal(600, ctx.TimeOfDay);
    }
}
```

- [ ] **Step 2: Run to verify it fails**

```bash
dotnet test tests/StardewBot.Tests/ --filter "DayContext_Spring_Day1"
```

Expected: compile error — `DayContext` and `Season` not found.

- [ ] **Step 3: Implement DayContext**

Create `src/StardewBot/GameState/DayContext.cs`:

```csharp
namespace StardewBot.GameState;

public enum Season { Spring, Summer, Fall, Winter }
public enum Weather { Sunny, Rainy, Windy, Snowy }

public record DayContext(Season Season, int Day, int TimeOfDay, Weather Weather = Weather.Sunny)
{
    public bool IsLateNight => TimeOfDay >= 2400;
    public bool IsAlmostNight => TimeOfDay >= 2200;
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/StardewBot.Tests/ --filter "DayContext_Spring_Day1"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/GameState/DayContext.cs tests/StardewBot.Tests/ScoringTests.cs
git commit -m "feat: add DayContext with Season and Weather enums"
```

---

## Task 3: IWorldReader Interface + FakeWorldReader

**Files:**
- Create: `src/StardewBot/GameState/IWorldReader.cs`
- Create: `src/StardewBot/GameState/NpcInfo.cs`
- Create: `tests/StardewBot.Tests/Fakes/FakeWorldReader.cs`

- [ ] **Step 1: Create NpcInfo record**

Create `src/StardewBot/GameState/NpcInfo.cs`:

```csharp
namespace StardewBot.GameState;

public record NpcInfo(
    string Name,
    int FriendshipHearts,
    bool IsBirthday,
    bool HasPreferredGiftAvailable
);
```

- [ ] **Step 2: Create IWorldReader interface**

Create `src/StardewBot/GameState/IWorldReader.cs`:

```csharp
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace StardewBot.GameState;

public interface IWorldReader
{
    float EnergyPercent { get; }            // 0.0 - 1.0
    float InventoryFillRatio { get; }       // 0.0 - 1.0
    int MineFloorReached { get; }           // deepest floor ever reached
    IReadOnlyList<Vector2> CropsToWater { get; }
    IReadOnlyList<Vector2> CropsToHarvest { get; }
    IReadOnlyList<Vector2> ForagablePositions { get; }
    IReadOnlyList<NpcInfo> Npcs { get; }
    bool IsRaining { get; }
}
```

- [ ] **Step 3: Create FakeWorldReader for tests**

Create `tests/StardewBot.Tests/Fakes/FakeWorldReader.cs`:

```csharp
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
```

- [ ] **Step 4: Build to verify no errors**

```bash
dotnet build src/StardewBot/
```

Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/GameState/ tests/StardewBot.Tests/Fakes/
git commit -m "feat: add IWorldReader interface and FakeWorldReader for tests"
```

---

## Task 4: IAction Interface + ScoreContext

**Files:**
- Create: `src/StardewBot/Scoring/IAction.cs`
- Create: `src/StardewBot/Scoring/ScoreContext.cs`

- [ ] **Step 1: Write failing test for ScoreContext**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
[Fact]
public void ScoreContext_AddsModifiersCorrectly()
{
    var score = new ScoreContext()
        .Add(30f)
        .AddIf(true, 20f)
        .AddIf(false, 50f);

    Assert.Equal(50f, score.Total);
}
```

- [ ] **Step 2: Run to verify it fails**

```bash
dotnet test tests/StardewBot.Tests/ --filter "ScoreContext_Adds"
```

Expected: compile error — `ScoreContext` not found.

- [ ] **Step 3: Implement ScoreContext**

Create `src/StardewBot/Scoring/ScoreContext.cs`:

```csharp
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
```

- [ ] **Step 4: Create IAction interface**

Create `src/StardewBot/Scoring/IAction.cs`:

```csharp
using StardewBot.GameState;

namespace StardewBot.Scoring;

public interface IAction
{
    string Name { get; }

    // Returns score 0-100+ (higher = higher priority today)
    float Score(DayContext ctx, IWorldReader world);

    // Called once when action is selected; capture state here
    void Begin(DayContext ctx, IWorldReader world);

    // Called each UpdateTicked; return true when the action is complete
    bool Tick();
}
```

- [ ] **Step 5: Run ScoreContext test to verify it passes**

```bash
dotnet test tests/StardewBot.Tests/ --filter "ScoreContext_Adds"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/StardewBot/Scoring/
git commit -m "feat: add IAction interface and ScoreContext additive builder"
```

---

## Task 5: FarmAction

**Files:**
- Create: `src/StardewBot/Scoring/FarmAction.cs`

- [ ] **Step 1: Write failing tests**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
using StardewBot.Scoring;
using StardewBot.Tests.Fakes;
using Microsoft.Xna.Framework;

[Fact]
public void FarmAction_ScoresHighWhenCropsNeedHarvesting()
{
    var world = new FakeWorldReader
    {
        CropsToHarvest = new List<Vector2> { new(1, 1) },
        EnergyPercent = 0.8f
    };
    var ctx = new DayContext(Season.Spring, 5, 800);
    var action = new FarmAction();

    float score = action.Score(ctx, world);

    Assert.True(score >= 40f, $"Expected >= 40, got {score}");
}

[Fact]
public void FarmAction_ScoresLowWhenNoCropsAndNoWatering()
{
    var world = new FakeWorldReader { EnergyPercent = 0.8f };
    var ctx = new DayContext(Season.Spring, 5, 800);
    var action = new FarmAction();

    float score = action.Score(ctx, world);

    Assert.True(score < 10f, $"Expected < 10, got {score}");
}

[Fact]
public void FarmAction_ScoresZeroWhenEnergyTooLow()
{
    var world = new FakeWorldReader
    {
        CropsToHarvest = new List<Vector2> { new(1, 1) },
        EnergyPercent = 0.1f
    };
    var ctx = new DayContext(Season.Spring, 5, 800);
    var action = new FarmAction();

    float score = action.Score(ctx, world);

    Assert.Equal(0f, score);
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "FarmAction"
```

Expected: compile error — `FarmAction` not found.

- [ ] **Step 3: Implement FarmAction**

Create `src/StardewBot/Scoring/FarmAction.cs`:

```csharp
using Microsoft.Xna.Framework;
using StardewBot.GameState;
using System.Collections.Generic;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewBot.Scoring;

public class FarmAction : IAction
{
    public string Name => "Farm";

    private Queue<Vector2>? _tilesToProcess;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.EnergyPercent < 0.2f) return 0f;

        return new ScoreContext()
            .AddIf(world.CropsToHarvest.Count > 0, 40f)
            .AddIf(world.CropsToWater.Count > 0, 30f)
            .AddIf(world.EnergyPercent > 0.5f, 5f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _tilesToProcess = new Queue<Vector2>();
        foreach (var tile in world.CropsToHarvest) _tilesToProcess.Enqueue(tile);
        foreach (var tile in world.CropsToWater) _tilesToProcess.Enqueue(tile);
    }

    public bool Tick()
    {
        if (_tilesToProcess == null || _tilesToProcess.Count == 0) return true;

        var tile = _tilesToProcess.Peek();
        var farm = Game1.getFarm();

        if (Game1.player.getTileLocation() == tile)
        {
            if (farm.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt)
            {
                if (dirt.readyForHarvest())
                    dirt.crop?.harvest((int)tile.X, (int)tile.Y, dirt);
                else if (dirt.state.Value == HoeDirt.dry)
                    dirt.state.Value = HoeDirt.watered;
            }
            _tilesToProcess.Dequeue();
            return _tilesToProcess.Count == 0;
        }

        Game1.player.controller = new PathFindController(
            Game1.player, Game1.getFarm(), tile.ToPoint(), 0
        );
        return false;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/StardewBot.Tests/ --filter "FarmAction"
```

Expected: all 3 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/Scoring/FarmAction.cs
git commit -m "feat: add FarmAction with harvest/water scoring"
```

---

## Task 6: MineAction

**Files:**
- Create: `src/StardewBot/Scoring/MineAction.cs`

- [ ] **Step 1: Write failing tests**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
[Fact]
public void MineAction_ScoresHighMidSeason()
{
    var world = new FakeWorldReader { InventoryFillRatio = 0.3f, EnergyPercent = 0.9f };
    var ctx = new DayContext(Season.Spring, 8, 800);
    var action = new MineAction();

    float score = action.Score(ctx, world);

    Assert.True(score >= 30f, $"Expected >= 30, got {score}");
}

[Fact]
public void MineAction_ScoresLowWhenInventoryFull()
{
    var world = new FakeWorldReader { InventoryFillRatio = 0.95f, EnergyPercent = 0.9f };
    var ctx = new DayContext(Season.Spring, 8, 800);
    var action = new MineAction();

    float score = action.Score(ctx, world);

    Assert.True(score < 10f, $"Expected < 10, got {score}");
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "MineAction"
```

Expected: compile error.

- [ ] **Step 3: Implement MineAction**

Create `src/StardewBot/Scoring/MineAction.cs`:

```csharp
using StardewBot.GameState;
using StardewValley;
using StardewValley.Locations;

namespace StardewBot.Scoring;

public class MineAction : IAction
{
    public string Name => "Mine";

    private bool _started;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.EnergyPercent < 0.3f) return 0f;
        if (world.InventoryFillRatio > 0.85f) return 0f;

        bool isMidSeason = ctx.Day is >= 5 and <= 20;

        return new ScoreContext()
            .AddIf(isMidSeason, 30f)
            .AddIf(world.MineFloorReached < 40, 15f)   // early progression bonus
            .AddIf(world.EnergyPercent > 0.7f, 10f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _started = false;
    }

    public bool Tick()
    {
        if (!_started)
        {
            // Navigate to mine entrance on the mountain
            var mountain = Game1.getLocationFromName("Mountain");
            var mineEntrance = new Microsoft.Xna.Framework.Point(124, 100);
            Game1.player.controller = new PathFindController(
                Game1.player, mountain, mineEntrance, 0,
                (c, loc) => Game1.enterMine(Math.Min(120, MineShaft.lowestLevelReached + 1))
            );
            _started = true;
            return false;
        }

        // Action completes after player enters mine; ModEntry will detect
        // energy drop and trigger SleepAction when needed.
        return Game1.currentLocation is MineShaft mine &&
               mine.mineLevel >= MineShaft.lowestLevelReached;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/StardewBot.Tests/ --filter "MineAction"
```

Expected: both PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/Scoring/MineAction.cs
git commit -m "feat: add MineAction with mid-season progression scoring"
```

---

## Task 7: FishAction

**Files:**
- Create: `src/StardewBot/Scoring/FishAction.cs`

- [ ] **Step 1: Write failing tests**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
[Fact]
public void FishAction_ScoresHigherOnRainyDay()
{
    var worldRainy = new FakeWorldReader { IsRaining = true, EnergyPercent = 0.9f };
    var worldSunny = new FakeWorldReader { IsRaining = false, EnergyPercent = 0.9f };
    var ctx = new DayContext(Season.Summer, 10, 800);
    var action = new FishAction();

    float rainyScore = action.Score(ctx, worldRainy);
    float sunnyScore = action.Score(ctx, worldSunny);

    Assert.True(rainyScore > sunnyScore, "Rainy should score higher than sunny");
}

[Fact]
public void FishAction_ScoresZeroInWinter()
{
    var world = new FakeWorldReader { EnergyPercent = 0.9f };
    var ctx = new DayContext(Season.Winter, 10, 800, Weather.Rainy);
    var action = new FishAction();

    float score = action.Score(ctx, world);

    Assert.Equal(0f, score);
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "FishAction"
```

Expected: compile error.

- [ ] **Step 3: Implement FishAction**

Create `src/StardewBot/Scoring/FishAction.cs`:

```csharp
using StardewBot.GameState;
using StardewValley;
using StardewValley.Tools;

namespace StardewBot.Scoring;

public class FishAction : IAction
{
    public string Name => "Fish";

    private bool _casting;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (ctx.Season == Season.Winter) return 0f;
        if (world.EnergyPercent < 0.3f) return 0f;

        return new ScoreContext()
            .Add(15f)
            .AddIf(world.IsRaining, 20f)
            .AddIf(ctx.Season == Season.Spring || ctx.Season == Season.Fall, 10f)
            .AddIf(world.InventoryFillRatio < 0.5f, 5f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _casting = false;
    }

    public bool Tick()
    {
        // Walk to forest river (reliable fishing spot, all seasons)
        var forest = Game1.getLocationFromName("Forest");
        var fishingSpot = new Microsoft.Xna.Framework.Point(57, 55);

        if (!_casting)
        {
            if (Game1.player.getTileLocation() != fishingSpot.ToVector2())
            {
                Game1.player.controller = new PathFindController(
                    Game1.player, forest, fishingSpot, 0
                );
                return false;
            }

            var rod = Game1.player.Items.OfType<FishingRod>().FirstOrDefault();
            if (rod == null) return true;
            Game1.player.CurrentTool = rod;
            Game1.player.BeginUsingTool();
            _casting = true;
            return false;
        }

        // FishingMinigameHandler takes over when BobberBar appears
        return false;
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/StardewBot.Tests/ --filter "FishAction"
```

Expected: both PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/Scoring/FishAction.cs
git commit -m "feat: add FishAction with rainy-day and season scoring"
```

---

## Task 8: ForageAction

**Files:**
- Create: `src/StardewBot/Scoring/ForageAction.cs`

- [ ] **Step 1: Write failing tests**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
[Fact]
public void ForageAction_ScoresHigherInSpringAndFall()
{
    var world = new FakeWorldReader
    {
        ForagablePositions = new List<Vector2> { new(5, 5) },
        EnergyPercent = 0.9f
    };
    var springCtx = new DayContext(Season.Spring, 5, 800);
    var summerCtx = new DayContext(Season.Summer, 5, 800);
    var action = new ForageAction();

    float springScore = action.Score(springCtx, world);
    float summerScore = action.Score(summerCtx, world);

    Assert.True(springScore > summerScore);
}

[Fact]
public void ForageAction_ScoresZeroWhenNoForagables()
{
    var world = new FakeWorldReader { EnergyPercent = 0.9f };
    var ctx = new DayContext(Season.Spring, 5, 800);
    var action = new ForageAction();

    Assert.Equal(0f, action.Score(ctx, world));
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "ForageAction"
```

Expected: compile error.

- [ ] **Step 3: Implement ForageAction**

Create `src/StardewBot/Scoring/ForageAction.cs`:

```csharp
using Microsoft.Xna.Framework;
using StardewBot.GameState;
using StardewValley;
using System.Collections.Generic;

namespace StardewBot.Scoring;

public class ForageAction : IAction
{
    public string Name => "Forage";

    private Queue<Vector2>? _targets;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.ForagablePositions.Count == 0) return 0f;
        if (world.EnergyPercent < 0.2f) return 0f;

        return new ScoreContext()
            .Add(10f)
            .AddIf(ctx.Season == Season.Spring || ctx.Season == Season.Fall, 15f)
            .AddIf(world.ForagablePositions.Count > 3, 10f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world)
    {
        _targets = new Queue<Vector2>(world.ForagablePositions);
    }

    public bool Tick()
    {
        if (_targets == null || _targets.Count == 0) return true;

        var tile = _targets.Peek();
        if (Game1.player.getTileLocation() == tile)
        {
            Game1.currentLocation.checkAction(
                new xTile.Dimensions.Location((int)tile.X, (int)tile.Y),
                Game1.viewport,
                Game1.player
            );
            _targets.Dequeue();
            return _targets.Count == 0;
        }

        Game1.player.controller = new PathFindController(
            Game1.player, Game1.currentLocation, tile.ToPoint(), 0
        );
        return false;
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/StardewBot.Tests/ --filter "ForageAction"
```

Expected: both PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/Scoring/ForageAction.cs
git commit -m "feat: add ForageAction with spring/fall season bonus"
```

---

## Task 9: SocialAction

**Files:**
- Create: `src/StardewBot/Scoring/SocialAction.cs`

- [ ] **Step 1: Write failing tests**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
[Fact]
public void SocialAction_ScoresHighOnNpcBirthday()
{
    var world = new FakeWorldReader
    {
        Npcs = new List<NpcInfo>
        {
            new("Abigail", FriendshipHearts: 4, IsBirthday: true, HasPreferredGiftAvailable: true)
        },
        EnergyPercent = 0.9f
    };
    var ctx = new DayContext(Season.Spring, 3, 800);
    var action = new SocialAction();

    float score = action.Score(ctx, world);

    Assert.True(score >= 50f);
}

[Fact]
public void SocialAction_ScoresZeroWhenAllNpcsMaxFriendship()
{
    var world = new FakeWorldReader
    {
        Npcs = new List<NpcInfo>
        {
            new("Robin", FriendshipHearts: 10, IsBirthday: false, HasPreferredGiftAvailable: false)
        },
        EnergyPercent = 0.9f
    };
    var ctx = new DayContext(Season.Spring, 3, 800);
    var action = new SocialAction();

    Assert.Equal(0f, action.Score(ctx, world));
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "SocialAction"
```

Expected: compile error.

- [ ] **Step 3: Implement SocialAction**

Create `src/StardewBot/Scoring/SocialAction.cs`:

```csharp
using StardewBot.GameState;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

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

        if (Game1.player.getTileLocation() != npc.getTileLocation())
        {
            Game1.player.controller = new PathFindController(
                Game1.player, Game1.currentLocation, npc.getTileLocationPoint(), 1
            );
            return false;
        }

        Game1.player.giftItem(npc);
        _targets.RemoveAt(0);
        return _targets.Count == 0;
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/StardewBot.Tests/ --filter "SocialAction"
```

Expected: both PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/Scoring/SocialAction.cs
git commit -m "feat: add SocialAction with birthday and gift scoring"
```

---

## Task 10: ShipAction + SleepAction

**Files:**
- Create: `src/StardewBot/Scoring/ShipAction.cs`
- Create: `src/StardewBot/Scoring/SleepAction.cs`

- [ ] **Step 1: Write failing tests**

Add to `tests/StardewBot.Tests/ScoringTests.cs`:

```csharp
[Fact]
public void ShipAction_ScoresHighWhenInventoryNearFull()
{
    var world = new FakeWorldReader { InventoryFillRatio = 0.9f };
    var ctx = new DayContext(Season.Spring, 5, 800);
    var action = new ShipAction();

    float score = action.Score(ctx, world);

    Assert.True(score >= 60f);
}

[Fact]
public void ShipAction_ScoresZeroWhenInventoryEmpty()
{
    var world = new FakeWorldReader { InventoryFillRatio = 0.1f };
    var ctx = new DayContext(Season.Spring, 5, 800);

    Assert.Equal(0f, new ShipAction().Score(ctx, world));
}

[Fact]
public void SleepAction_AlwaysScores999()
{
    var world = new FakeWorldReader { EnergyPercent = 0.05f };
    var ctx = new DayContext(Season.Spring, 5, 2400);

    Assert.Equal(999f, new SleepAction().Score(ctx, world));
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "ShipAction|SleepAction"
```

Expected: compile errors.

- [ ] **Step 3: Implement ShipAction**

Create `src/StardewBot/Scoring/ShipAction.cs`:

```csharp
using StardewBot.GameState;
using StardewValley;
using StardewValley.Buildings;

namespace StardewBot.Scoring;

public class ShipAction : IAction
{
    public string Name => "Ship";

    private bool _done;

    public float Score(DayContext ctx, IWorldReader world)
    {
        if (world.InventoryFillRatio < 0.8f) return 0f;
        return new ScoreContext()
            .Add(60f)
            .AddIf(world.InventoryFillRatio > 0.95f, 20f)
            .Total;
    }

    public void Begin(DayContext ctx, IWorldReader world) => _done = false;

    public bool Tick()
    {
        if (_done) return true;

        var farm = Game1.getFarm();
        var bin = farm.getBuildingByType("Shipping Bin");
        if (bin == null) { _done = true; return true; }

        var binTile = new Microsoft.Xna.Framework.Point(
            (int)bin.tileX.Value + 1, (int)bin.tileY.Value + 1
        );

        if (Game1.player.getTileLocation() != binTile.ToVector2())
        {
            Game1.player.controller = new PathFindController(
                Game1.player, farm, binTile, 0
            );
            return false;
        }

        foreach (var item in Game1.player.Items.ToList())
        {
            if (item != null) Game1.player.addItemToShippingBin(item);
        }
        _done = true;
        return true;
    }
}
```

- [ ] **Step 4: Implement SleepAction**

Create `src/StardewBot/Scoring/SleepAction.cs`:

```csharp
using StardewBot.GameState;
using StardewValley;

namespace StardewBot.Scoring;

public class SleepAction : IAction
{
    public string Name => "Sleep";

    private bool _done;

    // Always returns 999 — used only by ActionExecutor as an override, not by DailyPlanner
    public float Score(DayContext ctx, IWorldReader world) => 999f;

    public void Begin(DayContext ctx, IWorldReader world) => _done = false;

    public bool Tick()
    {
        if (_done) return true;

        var farmhouse = Game1.getLocationFromName("FarmHouse");
        var bedTile = new Microsoft.Xna.Framework.Point(21, 4);

        if (Game1.currentLocation.Name != "FarmHouse")
        {
            Game1.player.controller = new PathFindController(
                Game1.player, Game1.currentLocation,
                new Microsoft.Xna.Framework.Point(64, 15), 0,
                (c, loc) => Game1.warpFarmer("FarmHouse", 21, 4, false)
            );
            return false;
        }

        if (Game1.player.getTileLocation() != bedTile.ToVector2())
        {
            Game1.player.controller = new PathFindController(
                Game1.player, farmhouse, bedTile, 0
            );
            return false;
        }

        Game1.player.passOutFromTired = false;
        Game1.NewDay(0.01f);
        _done = true;
        return true;
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/StardewBot.Tests/ --filter "ShipAction|SleepAction"
```

Expected: all 3 PASS.

- [ ] **Step 6: Commit**

```bash
git add src/StardewBot/Scoring/ShipAction.cs src/StardewBot/Scoring/SleepAction.cs
git commit -m "feat: add ShipAction and SleepAction with guard scoring"
```

---

## Task 11: WorldReader (Real SMAPI Implementation)

**Files:**
- Create: `src/StardewBot/GameState/WorldReader.cs`

- [ ] **Step 1: Implement WorldReader using Game1 statics**

Create `src/StardewBot/GameState/WorldReader.cs`:

```csharp
using Microsoft.Xna.Framework;
using StardewValley;
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
                if (!npc.isVillager()) continue;
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
```

- [ ] **Step 2: Build to verify it compiles**

```bash
dotnet build src/StardewBot/
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/StardewBot/GameState/WorldReader.cs
git commit -m "feat: add WorldReader using Game1 statics"
```

---

## Task 12: DailyPlanner

**Files:**
- Create: `src/StardewBot/Planner/DailyPlanner.cs`
- Modify: `tests/StardewBot.Tests/PlannerTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/StardewBot.Tests/PlannerTests.cs`:

```csharp
using StardewBot.GameState;
using StardewBot.Planner;
using StardewBot.Scoring;
using StardewBot.Tests.Fakes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Xunit;

namespace StardewBot.Tests;

public class PlannerTests
{
    [Fact]
    public void DailyPlanner_PutsHighestScoringActionFirst()
    {
        var world = new FakeWorldReader
        {
            CropsToHarvest = new List<Vector2> { new(1, 1) },
            EnergyPercent = 0.9f,
            InventoryFillRatio = 0.1f
        };
        var ctx = new DayContext(Season.Spring, 5, 800);
        var planner = new DailyPlanner(new IAction[]
        {
            new FarmAction(),
            new ShipAction(),
        });

        var queue = planner.BuildQueue(ctx, world);

        Assert.Equal("Farm", queue[0].Name);
    }

    [Fact]
    public void DailyPlanner_ExcludesActionsWithZeroScore()
    {
        var world = new FakeWorldReader { EnergyPercent = 0.9f };
        var ctx = new DayContext(Season.Winter, 5, 800);
        var planner = new DailyPlanner(new IAction[]
        {
            new FishAction(),  // scores 0 in Winter
            new ForageAction() // scores 0 with no foragables
        });

        var queue = planner.BuildQueue(ctx, world);

        Assert.Empty(queue);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

```bash
dotnet test tests/StardewBot.Tests/ --filter "PlannerTests"
```

Expected: compile error — `DailyPlanner` not found.

- [ ] **Step 3: Implement DailyPlanner**

Create `src/StardewBot/Planner/DailyPlanner.cs`:

```csharp
using StardewBot.GameState;
using StardewBot.Scoring;
using System.Collections.Generic;
using System.Linq;

namespace StardewBot.Planner;

public class DailyPlanner
{
    private readonly IReadOnlyList<IAction> _actions;

    public DailyPlanner(IEnumerable<IAction> actions)
    {
        _actions = actions.ToList();
    }

    public IReadOnlyList<IAction> BuildQueue(DayContext ctx, IWorldReader world)
    {
        return _actions
            .Select(a => (Action: a, Score: a.Score(ctx, world)))
            .Where(x => x.Score > 0f)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Action)
            .ToList();
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/StardewBot.Tests/
```

Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/StardewBot/Planner/ tests/StardewBot.Tests/PlannerTests.cs
git commit -m "feat: add DailyPlanner that builds sorted action queue"
```

---

## Task 13: FishingMinigameHandler

**Files:**
- Create: `src/StardewBot/Executor/FishingMinigameHandler.cs`

- [ ] **Step 1: Implement the bobber controller**

Create `src/StardewBot/Executor/FishingMinigameHandler.cs`:

```csharp
using StardewValley;
using StardewValley.Menus;

namespace StardewBot.Executor;

public class FishingMinigameHandler
{
    public bool IsActive => Game1.activeClickableMenu is BobberBar;

    // Returns true while the minigame is still running.
    // Call each UpdateTicked while IsActive is true.
    public bool Tick()
    {
        if (Game1.activeClickableMenu is not BobberBar bar) return false;

        // BobberBar fields (accessed via reflection since they're private)
        float bobberPos = (float)typeof(BobberBar)
            .GetField("bobberPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(bar)!;
        float barPos = (float)typeof(BobberBar)
            .GetField("bobberBarPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(bar)!;
        float barHeight = (float)typeof(BobberBar)
            .GetField("bobberBarHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(bar)!;

        float barCenter = barPos + barHeight / 2f;
        bool shouldHold = bobberPos < barCenter;

        if (shouldHold)
            Game1.oldMouseState = new Microsoft.Xna.Framework.Input.MouseState(
                0, 0, 0, Microsoft.Xna.Framework.Input.ButtonState.Pressed,
                Microsoft.Xna.Framework.Input.ButtonState.Released,
                Microsoft.Xna.Framework.Input.ButtonState.Released,
                Microsoft.Xna.Framework.Input.ButtonState.Released,
                Microsoft.Xna.Framework.Input.ButtonState.Released
            );
        else
            Game1.oldMouseState = default;

        return true;
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

```bash
dotnet build src/StardewBot/
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/StardewBot/Executor/FishingMinigameHandler.cs
git commit -m "feat: add FishingMinigameHandler bobber PID controller"
```

---

## Task 14: ActionExecutor

**Files:**
- Create: `src/StardewBot/Executor/ActionExecutor.cs`

- [ ] **Step 1: Implement ActionExecutor**

Create `src/StardewBot/Executor/ActionExecutor.cs`:

```csharp
using StardewBot.GameState;
using StardewBot.Scoring;
using System.Collections.Generic;

namespace StardewBot.Executor;

public class ActionExecutor
{
    private readonly FishingMinigameHandler _fishHandler = new();
    private Queue<IAction>? _queue;
    private IAction? _current;
    private DayContext? _ctx;
    private IWorldReader? _world;

    public void StartDay(IReadOnlyList<IAction> queue, DayContext ctx, IWorldReader world)
    {
        _queue = new Queue<IAction>(queue);
        _ctx = ctx;
        _world = world;
        _current = null;
    }

    // Called every UpdateTicked. Returns false when nothing left to do.
    public bool Tick()
    {
        if (_ctx == null || _world == null) return false;

        // Energy and time guards — override current action
        bool energyLow = _world.EnergyPercent < 0.2f;
        bool almostNight = _ctx.IsAlmostNight;

        if ((energyLow || almostNight) && _current is not SleepAction)
        {
            var sleep = new SleepAction();
            sleep.Begin(_ctx, _world);
            _current = sleep;
        }

        // Inventory guard — inject ShipAction at front of queue
        if (_world.InventoryFillRatio > 0.95f && _current is not ShipAction && _current is not SleepAction)
        {
            var ship = new ShipAction();
            ship.Begin(_ctx, _world);
            var remaining = _queue != null ? _queue.ToArray() : System.Array.Empty<IAction>();
            _queue = new Queue<IAction>(new[] { ship }.Concat(remaining));
            _current = ship;
        }

        // Fishing minigame takes priority when active
        if (_fishHandler.IsActive)
        {
            _fishHandler.Tick();
            return true;
        }

        // Advance current action
        if (_current != null)
        {
            bool done = _current.Tick();
            if (!done) return true;
            _current = null;
        }

        // Pick next action from queue
        if (_queue == null || _queue.Count == 0) return false;

        _current = _queue.Dequeue();
        _current.Begin(_ctx, _world);
        return true;
    }
}
```

- [ ] **Step 2: Build to verify it compiles**

```bash
dotnet build src/StardewBot/
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/StardewBot/Executor/ActionExecutor.cs
git commit -m "feat: add ActionExecutor with energy/time/inventory guards"
```

---

## Task 15: ModEntry — Wire Everything Together

**Files:**
- Create: `src/StardewBot/ModEntry.cs`

- [ ] **Step 1: Implement ModEntry**

Create `src/StardewBot/ModEntry.cs`:

```csharp
using StardewBot.GameState;
using StardewBot.Executor;
using StardewBot.Planner;
using StardewBot.Scoring;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace StardewBot;

public class ModEntry : Mod
{
    private DailyPlanner? _planner;
    private ActionExecutor? _executor;
    private readonly WorldReader _world = new();

    public override void Entry(IModHelper helper)
    {
        _planner = new DailyPlanner(new IAction[]
        {
            new FarmAction(),
            new MineAction(),
            new FishAction(),
            new ForageAction(),
            new SocialAction(),
            new ShipAction(),
        });
        _executor = new ActionExecutor();

        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        var ctx = BuildContext();
        var queue = _planner!.BuildQueue(ctx, _world);
        _executor!.StartDay(queue, ctx, _world);
        Monitor.Log($"[StardewBot] Day {ctx.Day} {ctx.Season} — queue: {string.Join(", ", queue.Select(a => a.Name))}", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree) return;
        _executor?.Tick();
    }

    private DayContext BuildContext()
    {
        var season = Game1.currentSeason switch
        {
            "spring" => Season.Spring,
            "summer" => Season.Summer,
            "fall"   => Season.Fall,
            _        => Season.Winter
        };
        var weather = Game1.isRaining ? Weather.Rainy
                    : Game1.isSnowing ? Weather.Snowy
                    : Weather.Sunny;
        return new DayContext(season, Game1.dayOfMonth, Game1.timeOfDay, weather);
    }
}
```

- [ ] **Step 2: Build the full project**

```bash
dotnet build src/StardewBot/
```

Expected: build succeeds with 0 errors.

- [ ] **Step 3: Run all tests**

```bash
dotnet test tests/StardewBot.Tests/
```

Expected: all tests PASS.

- [ ] **Step 4: Install the mod**

```bash
# Linux path (adjust if needed)
MODS_DIR="$HOME/.local/share/Steam/steamapps/common/Stardew Valley/Mods"
mkdir -p "$MODS_DIR/StardewBot"
cp src/StardewBot/bin/Debug/net6.0/StardewBot.dll "$MODS_DIR/StardewBot/"
cp src/StardewBot/manifest.json "$MODS_DIR/StardewBot/"
```

- [ ] **Step 5: Launch Stardew Valley via SMAPI and verify the mod loads**

Start the game via SMAPI. In the SMAPI console output, you should see:

```
[StardewBot] LOADED
```

After loading a save and letting a day start:

```
[StardewBot] Day 1 Spring — queue: Farm, Mine, Forage
```

- [ ] **Step 6: Final commit**

```bash
git add src/StardewBot/ModEntry.cs
git commit -m "feat: add ModEntry wiring all layers to SMAPI events"
```

---

## Summary

| Layer | Key types | Testable without game? |
|---|---|---|
| GameState | `DayContext`, `IWorldReader`, `WorldReader` | DayContext + IWorldReader yes |
| Scoring | `IAction`, `ScoreContext`, 6× actions | Yes (FakeWorldReader) |
| Planner | `DailyPlanner` | Yes |
| Executor | `ActionExecutor`, `FishingMinigameHandler` | No (needs Game1) |
| Entry | `ModEntry` | No (needs SMAPI) |
