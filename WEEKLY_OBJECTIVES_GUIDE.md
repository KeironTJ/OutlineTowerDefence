# Weekly Objectives Implementation Guide

This implementation adds weekly objectives to the game, following the same pattern as daily objectives.

## Components Added

### 1. WeeklyObjectiveManager.cs
- Manages weekly objective lifecycle (assignment, progress, completion, claiming)
- Handles weekly slot rotation (configurable 7 or 14 day cycles)
- Listens to game events (enemy kills, rounds completed, currency earned/spent, etc.)
- Supports catch-up for missed weeks

### 2. PlayerData Updates
- Added `lastWeeklyObjectiveSlotKey` field to track the current weekly cycle

### 3. UI Integration
- Updated `MainRewardScreen` to populate weekly objectives tab
- Added weekly timer countdown display
- Separate panel map for weekly objectives

### 4. ObjectivePanelUI Updates
- Now supports both daily and weekly objectives
- Automatically calls the correct manager (Daily or Weekly) based on objective period

## Setup Instructions

### Unity Inspector Configuration

1. **Create Weekly Objective Definitions**
   - Right-click in Project window → Create → Rewards → Objective
   - Set `period` to `Weekly`
   - Configure objective type, rarity, target amount, and rewards
   - Weekly objectives should have higher rewards (cores, prisms, loops)

2. **WeeklyObjectiveManager GameObject**
   - Create a new GameObject in your scene (e.g., "WeeklyObjectiveManager")
   - Add the `WeeklyObjectiveManager` component
   - Configure settings:
     - `Max Weekly Objectives`: Number of concurrent weekly objectives (default: 3)
     - `Slot Length Days`: Weekly cycle length - 7 for weekly, 14 for biweekly (default: 7)
     - `Objectives Added Per Cycle`: How many objectives to add when a new week starts (default: 1)
     - `All Objectives`: Drag all your weekly ObjectiveDefinition assets here
     - `Remove Claimed On Next Cycle`: Whether to remove claimed objectives at week end (default: true)
     - `Grant Initial Fill`: Whether to add objectives immediately on first load (default: false)

3. **MainRewardScreen Updates**
   - In your MainRewardScreen GameObject:
     - Assign `weeklyObjectivesContentParent` - the Transform/ScrollView content where weekly panels spawn
     - Assign `nextWeeklySlotTimerText` - the TextMeshProUGUI to show weekly countdown

4. **Scene Setup**
   - The WeeklyObjectiveManager will persist across scenes (DontDestroyOnLoad)
   - Make sure it's initialized in your bootstrap scene

## Objective Types Supported

Weekly objectives support all the same types as daily objectives:

1. **KillEnemies** - Kill specific enemies (with filters for tier, family, traits)
2. **CompleteRounds** - Complete X rounds
3. **CompleteWaves** - Complete X waves
4. **EarnCurrency** - Earn X amount of a specific currency
5. **SpendCurrency** - Spend X amount of a specific currency
6. **UnlockSkill** - Unlock specific skills

## Weekly Objective Design Guidelines

Based on the issue requirements:

1. **Higher Volume/Difficulty**
   - Set higher `targetAmount` values than daily objectives
   - Example: "Kill 500 enemies" vs daily "Kill 50 enemies"

2. **Premium Rewards**
   - Use `rewardType`: Cores, Prisms, or Loops
   - Set higher `rewardAmount` values
   - Example: 1000+ cores, 50+ prisms, 10+ loops

3. **Special Objectives**
   - Can track weekly daily objective completions (future enhancement)
   - Long-term progression goals

## Weekly Cycle Logic

- **Week Start**: Monday 00:00 UTC
- **Cycle Length**: Configurable (7 or 14 days)
- **Rollover Behavior**: 
  - Claimed & completed objectives are removed
  - New objectives are added based on available slots
  - Progress on unclaimed objectives is preserved

## Events

The WeeklyObjectiveManager fires two events:

1. `OnProgress` - Fired when objective progress changes
2. `OnSlotRollover` - Fired when the weekly cycle resets

## Time Helpers

Available public methods for UI:
- `GetCurrentSlotStartUtc()` - When did current week start
- `GetNextSlotStartUtc()` - When does next week start  
- `GetTimeUntilNextSlot()` - TimeSpan until next week
- `GetSecondsUntilNextSlot()` - Seconds until next week
- `GetNextSlotCountdownString()` - Formatted countdown (e.g., "5d 12h 30m")

## Integration Notes

- Works alongside DailyObjectiveManager (both can run simultaneously)
- Uses the same ObjectiveProgressData serialization format
- Shares the same ObjectivePanelUI prefab
- Automatically saves to cloud via CloudSyncService
