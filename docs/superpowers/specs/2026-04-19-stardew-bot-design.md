# Stardew Valley Bot — Design Spec
**Date:** 2026-04-19
**Status:** Approved

## Overview

A SMAPI mod written in C# that fully auto-plays Stardew Valley indefinitely, using a utility-AI scoring system to decide what to do each day. The bot farms, mines, fishes, forages, and socializes — optimizing for gold, relationships, and skills with no fixed end goal.

---

## Architecture

The mod is structured in four clean layers. Each layer has one responsibility and communicates through well-defined interfaces.

```
StardewBot (SMAPI Mod)
├── ModEntry.cs
├── GameState/
│   ├── WorldReader.cs
│   └── DayContext.cs
├── Scoring/
│   ├── IAction.cs
│   ├── FarmAction.cs
│   ├── MineAction.cs
│   ├── FishAction.cs
│   ├── ForageAction.cs
│   ├── SocialAction.cs
│   └── ShipAction.cs
├── Planner/
│   └── DailyPlanner.cs
└── Executor/
    └── ActionExecutor.cs
```

### Layer Responsibilities

- **GameState** — reads current world: crops, NPC locations, energy, inventory, season, day, weather, time
- **Scoring** — each `IAction` implementation scores itself (0–100 float) given the current context
- **Planner** — runs all scorers at `DayStarted`, sorts by score, builds the day's action queue
- **Executor** — works through the queue each `UpdateTicked`, driving player movement and input

---

## Utility Scoring System

Each `IAction` exposes two methods:
```csharp
float Score(DayContext ctx, WorldReader world);
void Execute(DayContext ctx, WorldReader world);
```

Scores are additive: a `ScoreContext` helper accumulates modifiers so no weights are buried in conditionals.

| Action | Key scoring factors |
|---|---|
| `FarmAction` | Crops ready to harvest (+40), unwatered crops (+30), energy > 30% |
| `MineAction` | Day 5–14 of season, backpack space, current mine floor progress |
| `FishAction` | Rainy day bonus (+20), season fish availability, bundle fish needed |
| `SocialAction` | NPC birthday today (+50), friendship < 6 hearts, preferred gift in bag |
| `ForageAction` | Spring/Fall season bonus, foragables visible on map |
| `ShipAction` | Inventory > 80% full (always scores high to prevent overflow) |

---

## Executor & Movement

`ActionExecutor` drives the player per tick via a **waypoint queue**:

1. `Pathfinder` resolves a tile target using Stardew's built-in `PathFindController`
2. Player moves tile-by-tile via `Game1.player.setMovementDirection()`
3. On arrival, action is performed (`useToolOnTile`, `checkAction`)
4. Executor pops next waypoint or marks action complete

### Energy & Time Guards (middleware in executor loop)

- Energy < 20% → `SleepAction` forced with score +999, player walks to bed
- In-game time > 1:50am → same forced sleep override
- Inventory full → `ShipAction` injected at front of queue

### Fishing Minigame

Fishing requires a dedicated `FishingMinigameHandler` that reads `BobberBar` position each tick and clicks to keep the bar centered — a PID-style controller on the bobber position.

---

## Project Structure

```
stardewvalley/
├── src/
│   └── StardewBot/
│       ├── StardewBot.csproj       ← targets netstandard2.0
│       ├── manifest.json           ← SMAPI mod manifest
│       └── **/*.cs
├── tests/
│   └── StardewBot.Tests/
│       ├── ScoringTests.cs
│       └── PlannerTests.cs
└── docs/
    └── superpowers/specs/
```

## Dependencies

- SMAPI 4.x (NuGet)
- Stardew Valley game DLLs (local references, not redistributed)
- xUnit for scorer unit tests (no game required)

## Deployment

Build output goes to `%AppData%/StardewValley/Mods/StardewBot/` — standard SMAPI mod install path.

---

## Out of Scope

- GUI or config file for tuning scores (can be added later)
- Multiplayer support
- Combat AI beyond basic mine descending
