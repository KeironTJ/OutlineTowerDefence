# Stats Enrichment Implementation Guide

## Overview
This document explains the enhanced stats tracking system implemented to provide players with complete visibility of their performance across rounds.

## What's Been Added

### 1. New Event System
Two new event payload classes have been created to carry enriched metadata:

**`BulletFiredEvent`** - Triggered when a turret fires a bullet
- `Bullet bullet` - The bullet instance
- `string turretId` - ID of the turret that fired
- `string projectileId` - ID of the projectile definition used
- `float baseDamage` - Base damage of the shot

**`DamageDealtEvent`** - Triggered when a bullet damages an enemy
- `string projectileId` - ID of the projectile that dealt damage
- `float damageAmount` - Amount of damage dealt
- `bool wasCritical` - Whether this was a critical hit
- `string enemyDefinitionId` - ID of the enemy damaged

### 2. Extended Round Tracking

**New Fields in `RoundRecord`:**
```csharp
public string towerBaseId;                                    // Tower base used
public List<TurretUsageSummary> turretUsage;                  // Shots per turret
public List<ProjectileUsageSummary> projectileUsage;          // Shots and damage per projectile
public float totalDamageDealt;                                // Total damage in round
public int criticalHits;                                      // Number of critical hits
```

**New Summary Structures:**
```csharp
public struct TurretUsageSummary {
    public string turretId;
    public int shotsFired;
}

public struct ProjectileUsageSummary {
    public string projectileId;
    public int shotsFired;
    public float damageDealt;
}
```

### 3. RoundManager Tracking

The `RoundManager` now maintains several dictionaries during gameplay:
- `shotsByProjectile` - Tracks shots per projectile type
- `shotsByTurret` - Tracks shots per turret type
- `damageByProjectile` - Tracks damage per projectile type
- `totalDamageDealt` - Total damage for the round
- `criticalHitsThisRound` - Count of critical hits

These are populated via event handlers:
- `OnBulletFired()` - Increments shot counts
- `OnDamageDealt()` - Accumulates damage and tracks crits

### 4. Lifetime Stats in PlayerData

**New Fields:**
```csharp
public List<ProjectileUsageSummary> lifetimeProjectileStats;
public List<TurretUsageSummary> lifetimeTurretStats;
public float lifetimeTotalDamage;
public int lifetimeCriticalHits;
```

These accumulate when `OnRoundRecordUpdated()` is called in `PlayerManager`.

### 5. UI Updates

**RoundStatsView (Round Summary Panel):**
Now displays:
- Tower Base used
- Combat Stats section (Total Damage, Critical Hits)
- Projectile Usage (shots and damage per projectile)
- Turret Usage (shots per turret)

**GameStatsPanel (Player Lifetime Stats):**
Now displays:
- Combat Stats section (lifetime damage and crits)
- Top 10 projectiles by usage
- Top 10 turrets by usage

## How It Works

### Data Flow
```
1. Turret fires → BulletFiredEvent with metadata
2. RoundManager captures shot counts by turret/projectile
3. Bullet hits enemy → DamageDealtEvent with damage info
4. RoundManager accumulates damage stats
5. Round ends → RoundRecord created with all stats
6. PlayerManager accumulates lifetime stats from RoundRecord
7. UI panels display both live and historical data
```

### Example: Tracking a Shot
1. `Turret.Shoot()` creates a `BulletFiredEvent` with turret ID and projectile ID
2. `RoundManager.OnBulletFired()` increments counters in tracking dictionaries
3. Bullet collides with enemy, triggers `DamageDealtEvent`
4. `RoundManager.OnDamageDealt()` adds to damage totals and crit counter
5. Data is available in real-time via `GetLiveRoundRecord()`

### Example: Viewing Stats
**During Round:**
- `RoundStatsView` calls `RoundManager.GetLiveRoundRecord()` 
- Displays live updating stats

**After Round:**
- `RoundRecord` is created in `EndRound()`
- Saved to `PlayerData.RoundHistory`
- Lifetime stats updated in `PlayerManager.OnRoundRecordUpdated()`

**In Stats Panel:**
- `GameStatsPanel.PopulateStats()` reads from `PlayerData`
- Shows accumulated lifetime statistics

## Testing Checklist

When testing this implementation:

1. **Start a round** - Verify no errors on round start
2. **Fire some shots** - Check that `bulletsFiredThisRound` increments
3. **Kill enemies** - Verify damage is being tracked
4. **Get critical hits** - Check crit counter increases
5. **View round stats during play** - Should show live updating data
6. **Complete a round** - Verify round record is created with all stats
7. **View player stats** - Lifetime totals should include the round's data
8. **Play multiple rounds** - Verify stats accumulate correctly

## Potential Issues & Solutions

**Issue:** Stats not updating
- **Check:** Event listeners are registered in `OnEnable()`
- **Check:** Events are being triggered with correct payload types

**Issue:** Missing projectile/turret IDs
- **Check:** Turrets have `activeDefinition` set
- **Check:** Projectiles have valid `projectileDefinitionId`

**Issue:** Damage not tracking
- **Check:** `Bullet.OnCollisionEnter2D()` triggers `DamageDealtEvent`
- **Check:** `RoundManager` is listening to `EventNames.DamageDealt`

**Issue:** Lifetime stats not accumulating
- **Check:** `PlayerManager.OnRoundRecordUpdated()` is being called
- **Check:** Structs are being properly updated (they're value types)

## Future Enhancements

Possible additions to this system:
1. **Damage per enemy type** - Track which projectiles work best against which enemies
2. **Efficiency metrics** - Damage per shot, accuracy percentages
3. **Time-based stats** - Damage per minute, shots per minute
4. **Comparison views** - Compare rounds side-by-side
5. **Achievements** - Unlock achievements for stat milestones

## Files Modified

Core Logic:
- `Code/Scripts/Gameplay/RoundManager/RoundData.cs` - Added new structures
- `Code/Scripts/Gameplay/RoundManager/RoundManager.cs` - Added tracking logic
- `Code/Scripts/Gameplay/Turret/Turret.cs` - Enhanced event data
- `Code/Scripts/Gameplay/Projectile/Bullet.cs` - Added damage event
- `Code/Scripts/Gameplay/Player/PlayerData.cs` - Added lifetime fields
- `Code/Scripts/Gameplay/Player/PlayerManager.cs` - Added accumulation logic

Events:
- `Code/Scripts/Events/EventNames.cs` - Added DamageDealt event
- `Code/Scripts/Events/Payloads/BulletFiredEvent.cs` - New payload class
- `Code/Scripts/Events/Payloads/DamageDealtEvent.cs` - New payload class

UI:
- `Code/Scripts/UI/GlobalUI/RoundStatsUI/RoundStatsView.cs` - Enhanced display
- `Code/Scripts/UI/GlobalUI/PlayerUI/GameStatsPanel.cs` - Enhanced display

## Summary

This implementation provides comprehensive stat tracking that gives players full visibility into:
- Which weapons they're using and how effective they are
- How much damage they're dealing overall and per weapon type
- Which turrets are doing the heavy lifting
- Their critical hit performance
- Historical trends across all rounds played

All tracking is automatic, non-intrusive, and provides both real-time and historical views of performance data.
