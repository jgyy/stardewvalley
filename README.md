# StardewBot

A SMAPI mod that auto-plays Stardew Valley using a utility-AI scoring system. Each day the bot scores all possible activities (farming, mining, fishing, foraging, socialising, shipping) and works through them in priority order.

## Prerequisites

- Stardew Valley (Steam)
- [SMAPI 4.x](https://smapi.io) — download and run the installer
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for building from source

## Build

```bash
git clone <repo>
cd stardewvalley
dotnet build src/StardewBot/
```

The build automatically copies the mod to your Stardew Valley mods folder.

## Run

1. Launch Stardew Valley through SMAPI (not Steam directly).  
   On Linux: `~/.local/share/Steam/steamapps/common/Stardew\ Valley/StardewModdingAPI`
2. Load or create a save file — the bot activates immediately on the first day.
3. Watch the SMAPI console for the daily action queue:
   ```
   [StardewBot] Day 1 Spring — queue: Farm, Mine, Forage
   ```

## Test

```bash
dotnet test tests/StardewBot.Tests/
```

18 tests covering all action scorers and the daily planner. No game install required to run tests.

## How it works

Each in-game day `DailyPlanner` scores every action against the current world state and queues them highest-first. `ActionExecutor` works through the queue each tick, with hard overrides for low energy (→ sleep) and full inventory (→ ship).

| Action | Scores high when |
|---|---|
| Farm | Crops ready to harvest or need watering |
| Mine | Mid-season (days 5–20), inventory not full |
| Fish | Rainy day, Spring or Fall |
| Forage | Foragables visible, Spring or Fall |
| Social | NPC birthday today, gifts available |
| Ship | Inventory > 80% full |
