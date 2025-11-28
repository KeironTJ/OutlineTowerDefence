# Weekly Objectives Guide

The weekly system now delivers a fixed progression track that rewards players for consistently clearing daily objectives while still supporting traditional weekly tasks. This document explains the runtime behaviour, configuration points, and debug helpers that ship with the latest implementation.

## Feature Overview

- **Eight-tier progression track** that awards currency when players finish 3, 6, 10, 15, 20, 30, 40, and 55 daily objectives during the current week.
- **Daily completion tracking**: `DailyObjectiveManager` raises `OnDailyObjectiveCompleted` which the weekly manager listens to in order to advance the tier objectives.
- **Automatic weekly reset** every Monday 00:00 UTC (or according to `slotLengthDays`) that rebuilds the tier list and clears the completion counter.
- **Manual debug utilities** allowing testers to force a new week or dump current state directly from the Inspector.
- **Legacy weekly definitions** are still supported; any ScriptableObject added to `allObjectives` continues to work alongside the tier track.

## PlayerData Fields

The weekly system persists its state through the following fields on `PlayerData`:

- `weeklyObjectives` – active weekly objective progress entries.
- `lastWeeklyObjectiveSlotKey` – identifies the current weekly cycle (formatted `yyyyMMdd`).
- `weeklyDailyCompletions` – running count of daily objectives completed this week.

These values are saved automatically via `SaveManager` and uploaded through `CloudSyncService` when changes occur.

## WeeklyObjectiveManager Highlights

- Subscribes to the daily manager’s completion event to increment the weekly counter and update tier progress immediately.
- Recreates the eight tier objectives every time a new cycle begins. Each tier is generated at runtime through `CreateTierObjectiveDefinition`, so no ScriptableObject authoring is required for the completion track.
- Continues to evaluate other weekly objectives (kill enemies, currency spend, etc.) that are loaded from Resources into `allObjectives`.
- Provides `GetOrderedWeeklyObjectives()` which returns a completion-first ordering suitable for UI lists (unfinished objectives first).

### Tier Rewards

| Tier | Daily Completions | Reward (Cores) |
|-----:|-------------------|----------------|
|  1   | 3                 | 50             |
|  2   | 6                 | 120            |
|  3   | 10                | 250            |
|  4   | 15                | 450            |
|  5   | 20                | 700            |
|  6   | 30                | 1200           |
|  7   | 40                | 1800           |
|  8   | 55                | 2800           |

Rewards scale with difficulty; adjust `CalculateTierReward` if different pacing is desired.

## Configuration Checklist

1. **Scene Setup**
   - Add `WeeklyObjectiveManager` to a bootstrap scene (it marks itself `DontDestroyOnLoad`).
   - Toggle `slotLengthDays` if you want bi-weekly cycles.

2. **Optional Weekly Definitions**
   - Place additional weekly `ObjectiveDefinition` assets in `Resources/Data/Objectives/Weekly` and assign them to the `allObjectives` list if you want traditional weekly goals alongside the tier track.

3. **UI Wiring**
   - Ensure your rewards screen calls `GetOrderedWeeklyObjectives()` so incomplete tiers appear first.
   - Display the countdown using the provided helpers: `GetNextSlotCountdownString()` or `GetTimeUntilNextSlot()`.

4. **Debug Controls (Testing Only)**
   - Enable the `enableDebugControls` toggle in the Inspector to allow runtime triggering outside of the editor.
   - Use the context-menu buttons on the component:
     - **Force New Week (Debug)** – immediately resets the cycle, clears progress, and rebuilds tiers.
     - **Debug Current State** – logs the week key, completion count, and status of all tiers.
   - You can also call `WeeklyObjectiveManager.main.TriggerNewWeekForTesting()` from scripts or a debug console.

## Cycle Behaviour

- `EvaluateSlots()` runs every minute (configurable) to detect when the stored slot key differs from the current week.
- On rollover the manager:
  1. Clears `weeklyObjectives` and `activeWeekly`.
  2. Resets the daily completion counter back to zero.
  3. Rebuilds the eight tier objectives.
  4. Raises `OnSlotRollover` so UI or analytics code can react.

## Integration Best Practices

- **Daily objectives dependency**: The weekly track assumes `DailyObjectiveManager` is active so that it can receive `OnDailyObjectiveCompleted` events. Ensure both managers are loaded together (see `DAILY_OBJECTIVES_GUIDE.md`).
- **Ordering in UI**: Prefer the `GetOrderedWeeklyObjectives()` helper to keep incomplete tiers visible.
- **Save cadence**: The manager queues saves when progress updates, but if you batch claim rewards consider calling `SaveManager.main.QueueImmediateSave()` afterwards.
- **Extending rewards**: The default implementation grants cores; for multi-currency rewards adjust `CalculateTierReward` or extend `CreateTierObjectiveDefinition` to produce richer reward payloads.

## Reference API

- `GetCurrentSlotStartUtc()`, `GetNextSlotStartUtc()`, `GetTimeUntilNextSlot()`, `GetSecondsUntilNextSlot()` – time helpers for countdowns.
- `GetCurrentSlotKey()` – current weekly key (formatted string).
- `LogCurrentState()` – quick logging helper for diagnostics.
- `OnProgress` event – fired whenever progress updates, including tier completions.
- `OnSlotRollover` event – fired when a new week is seeded via schedule or debug trigger.

With these mechanics in place the weekly track offers clear long-term goals, transparent rewards, and tooling for rapid iteration during development.
