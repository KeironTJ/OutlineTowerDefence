# Daily Objectives Guide

This guide documents the refreshed daily objective system and how it pairs with the weekly progression track.

## Feature Overview

- **Rolling slot system** that evaluates objectives on a recurring cadence (`slotLengthHours`, default 6 hours).
- **Objective pool loading** from `Resources/Data/Objectives/Daily` or assets assigned in the inspector.
- **Automatic catch-up** that processes missed slot boundaries when the player returns after downtime.
- **Completion-first ordering helpers** for UI lists.
- **`OnDailyObjectiveCompleted` event** that notifies other systems (e.g., weekly tiers) when a daily objective is finished.

## PlayerData Fields

Daily progress is persisted through these fields on `PlayerData`:

- `dailyObjectives` – list of active objective progress entries.
- `lastDailyObjectiveSlotKey` – identifies the current slot key (generated via `SlotTimeUtility`).
- `lastDailyObjectiveAddIsoUtc` – timestamp of the last assignment (optional use).
- `weeklyDailyCompletions` – shared counter updated by the weekly system whenever a daily objective is completed.

## DailyObjectiveManager Highlights

- Loads definitions on demand; if the inspector list is empty, it searches `Resources/Data/Objectives/Daily`.
- Persists across scenes via `DontDestroyOnLoad` and waits for `SaveManager` before initializing.
- Evaluates slot rollover every 10 seconds (configurable) and can prune claimed objectives automatically when a new slot begins.
- Provides two ordering helpers:
  - `GetActiveDailyObjectives()` – raw list.
  - `GetActiveDailyObjectivesOrdered()` – incomplete objectives first, then by progress fraction.
- Raises `OnProgress` whenever an objective updates, allowing UI to refresh progress bars.
- Invokes `OnDailyObjectiveCompleted` when an objective reaches its target so the weekly manager can advance its tiers.

## Configuration Checklist

1. **Scene Setup**
   - Place a `DailyObjectiveManager` in your bootstrap scene (or prefab). It is self-contained and marks itself `DontDestroyOnLoad`.
   - Ensure the corresponding `EventManager` events are fired by gameplay systems (enemy kills, currency earn/spend, etc.).

2. **Objective Pool**
   - Author `ObjectiveDefinition` assets under `Resources/Data/Objectives/Daily`.
   - Set `period` to `Daily` and configure the appropriate reward type and amount.
   - Optionally assign a curated list via the inspector if you do not want to load from Resources.

3. **Slot Configuration**
   - `slotLengthHours` controls how often new objectives can spawn (default 6 hours).
   - `maxDailyObjectives` limits simultaneous active objectives (default 4).
   - `objectivesAddedPerCycle` defines how many new objectives are introduced at each slot rollover.
   - Toggle `grantInitialFill` if you want players to start with a full set on first login.

4. **Pruning Behaviour**
   - If `removeClaimedOnNextCycle` is true, completed-and-claimed objectives are pruned during the next slot evaluation. Leave disabled if you prefer manual clearing.

## Integration with Weekly Track

- The weekly manager subscribes to `OnDailyObjectiveCompleted`, so completing any daily objective immediately increments the weekly completion counter.
- When designing UI, display both tracks together to highlight progress: call `DailyObjectiveManager.main.GetActiveDailyObjectivesOrdered()` and `WeeklyObjectiveManager.main.GetOrderedWeeklyObjectives()`.
- Ensure both managers are instantiated in the same scene so event wiring occurs correctly.

## Debugging Tips

- Use the `Debug` logging in `DailyObjectiveManager` (commented throughout the code) to trace slot rollover behaviour if needed.
- The weekly manager offers Inspector buttons (`Force New Week`, `Debug Current State`) that depend on the daily manager firing completion events. Verify those events fire by watching the console output after completing objectives.

With these pieces in place, the daily system provides a steady cadence of short-term goals that seamlessly feed the weekly tier progression.
