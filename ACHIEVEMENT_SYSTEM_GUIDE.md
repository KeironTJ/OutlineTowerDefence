# Achievement System - Complete Guide

## Overview

The Achievement System provides a comprehensive framework for creating stackable, multi-tiered achievements that reward players with currencies, unlocks, and other benefits. It's designed to support long-term player engagement and progression.

---

## System Components

### 1. AchievementDefinition (ScriptableObject)

Configurable achievement templates that define:
- Achievement identity and category
- Multiple tiers with progressive targets
- Filters for specific criteria
- Rewards per tier
- UI presentation

**Location:** `Resources/Data/Achievements/`

### 2. AchievementProgressData (Serializable)

Tracks player progress for each achievement:
- Current progress amount
- Highest tier completed
- Last updated timestamp

**Persisted in:** `PlayerData.achievementProgress`

### 3. AchievementRuntime

Runtime wrapper combining definition and progress:
- Provides convenience methods
- Calculates completion status
- Determines current/next tier

### 4. AchievementManager (Singleton)

Central manager that:
- Loads achievements from Resources
- Tracks progress via events
- Grants rewards on tier completion
- Persists data to SaveManager

---

## Achievement Types

| Type | Description | Progress Tracking |
|------|-------------|-------------------|
| **KillEnemies** | Destroy enemies | Increments per enemy kill (filterable) |
| **ShootProjectiles** | Fire projectiles | Increments per bullet fired |
| **CompleteWaves** | Finish waves | Increments per wave completed |
| **CompleteRounds** | Finish rounds | Increments per round completed |
| **ReachDifficulty** | Reach wave milestones | Tracks highest wave reached |
| **EarnCurrency** | Earn specific currency | Accumulates currency earned |
| **SpendCurrency** | Spend specific currency | Accumulates currency spent |
| **UnlockTurret** | Unlock turrets | Manual progression |
| **UnlockProjectile** | Unlock projectiles | Manual progression |
| **UpgradeProjectile** | Upgrade projectiles | Manual progression |

---

## Achievement Categories

- **Combat**: Enemy kills, projectile usage
- **Progression**: Waves, rounds, difficulty
- **Economy**: Currency earning/spending
- **Mastery**: Advanced unlocks and upgrades

---

## Tier System

Achievements support multiple stacking tiers for progressive milestones:

```
Example: "Enemy Eliminator"
├── Tier 1 (Bronze): Kill 10 enemies → Reward: 100 Cores
├── Tier 2 (Silver): Kill 100 enemies → Reward: 500 Cores
├── Tier 3 (Gold): Kill 1000 enemies → Reward: 2000 Cores + Unlock Turret
└── Tier 4 (Platinum): Kill 10000 enemies → Reward: 10000 Cores + Unlock Projectile
```

**Key Features:**
- Automatic progression through tiers
- Each tier can have multiple rewards
- Tiers are sorted by target amount
- Progress is cumulative (not reset per tier)

---

## Creating Achievements

### Step 1: Create Achievement Asset

1. Right-click in Unity Project → **Create → Rewards → Achievement**
2. Name it descriptively (e.g., "ACH_EnemyKiller")
3. Move to `Resources/Data/Achievements/`

### Step 2: Configure Identity

```
id: "ACH_ENEMY_KILLER"
displayName: "Enemy Eliminator"
description: "Destroy enemies to prove your combat prowess"
category: Combat
type: KillEnemies
```

### Step 3: Define Tiers

```
Tiers (4 elements):
├── [0] Bronze
│   ├── tierLevel: 1
│   ├── targetAmount: 10
│   ├── tierName: "Bronze"
│   ├── tierDescription: "Kill 10 enemies"
│   └── rewards: [100 Cores]
├── [1] Silver
│   ├── tierLevel: 2
│   ├── targetAmount: 100
│   ├── tierName: "Silver"
│   ├── tierDescription: "Kill 100 enemies"
│   └── rewards: [500 Cores]
├── [2] Gold
│   ├── tierLevel: 3
│   ├── targetAmount: 1000
│   ├── tierName: "Gold"
│   ├── tierDescription: "Kill 1000 enemies"
│   └── rewards: [2000 Cores, Unlock STD_TURRET]
└── [3] Platinum
    ├── tierLevel: 4
    ├── targetAmount: 10000
    ├── tierName: "Platinum"
    ├── tierDescription: "Kill 10000 enemies"
    └── rewards: [10000 Cores, Unlock CHAIN_BULLET]
```

### Step 4: Configure Filters (Optional)

For **KillEnemies** type:
```
targetDefinitionId: "BOSS_01" // Specific enemy (leave empty for any)
useTierFilter: true
targetEnemyTier: Boss
useFamilyFilter: true
targetFamily: "Mechanical"
useTraitFilter: true
targetTraits: Armored | Fast
```

For **EarnCurrency** / **SpendCurrency**:
```
currencyType: Cores
```

For **ShootProjectiles** (future):
```
targetProjectileId: "EXPLOSIVE_BOLT"
targetProjectileTrait: Explosive
```

---

## Reward Types

### Currency Reward
```
rewardType: Currency
currencyType: Cores
amount: 1000
```

### Unlock Turret
```
rewardType: UnlockTurret
rewardId: "STD"
```

### Unlock Projectile
```
rewardType: UnlockProjectile
rewardId: "CHAIN_BULLET"
```

### Unlock Tower Base
```
rewardType: UnlockTowerBase
rewardId: "0004"
```

### Stat Bonus (Future)
```
rewardType: StatBonus
rewardId: "damage_boost_5pct"
```

---

## Setup Instructions

### Scene Setup

1. Create empty GameObject → Name: "AchievementManager"
2. Add Component → **AchievementManager**
3. Leave `allAchievements` empty (auto-loads from Resources)

### Create Sample Achievement

See **Example Achievements** section below.

### Test in Play Mode

1. Enter Play Mode
2. Trigger achievement criteria (kill enemies, complete waves, etc.)
3. Check Console for completion messages
4. Verify rewards granted in PlayerData

---

## Example Achievements

### Combat: Enemy Slayer
```yaml
id: ACH_ENEMY_SLAYER
type: KillEnemies
category: Combat
tiers:
  - Bronze (10 kills): 100 Cores
  - Silver (100 kills): 500 Cores
  - Gold (1000 kills): 2000 Cores
  - Platinum (10000 kills): 10000 Cores + Unlock "Chain Bullet"
filters: None (any enemy)
```

### Combat: Boss Hunter
```yaml
id: ACH_BOSS_HUNTER
type: KillEnemies
category: Combat
tiers:
  - Bronze (1 boss): 500 Cores
  - Silver (10 bosses): 2500 Cores
  - Gold (50 bosses): 10000 Cores + Unlock "Boss Buster Turret"
filters: 
  useTierFilter: true
  targetEnemyTier: Boss
```

### Progression: Wave Master
```yaml
id: ACH_WAVE_MASTER
type: CompleteWaves
category: Progression
tiers:
  - Bronze (10 waves): 200 Prisms
  - Silver (100 waves): 1000 Prisms
  - Gold (500 waves): 5000 Prisms
  - Diamond (2000 waves): 20000 Prisms + 100 Loops
```

### Progression: Difficulty Conqueror
```yaml
id: ACH_DIFFICULTY_MASTER
type: ReachDifficulty
category: Progression
tiers:
  - Novice (Wave 10): 500 Cores
  - Intermediate (Wave 25): 2000 Cores
  - Expert (Wave 50): 10000 Cores + Unlock "Advanced Turret"
  - Master (Wave 100): 50000 Cores + Unlock "Elite Projectile"
```

### Combat: Trigger Happy
```yaml
id: ACH_PROJECTILE_SPAM
type: ShootProjectiles
category: Combat
tiers:
  - Bronze (1000 shots): 100 Cores
  - Silver (10000 shots): 500 Cores
  - Gold (100000 shots): 2500 Cores
  - Platinum (1000000 shots): 15000 Cores + 50 Loops
```

### Economy: Wealthy Collector
```yaml
id: ACH_CORE_COLLECTOR
type: EarnCurrency
category: Economy
currencyType: Cores
tiers:
  - Bronze (1000 Cores): 500 Prisms
  - Silver (10000 Cores): 2000 Prisms
  - Gold (100000 Cores): 10000 Prisms
  - Platinum (1000000 Cores): 50000 Prisms + 100 Loops
```

---

## Integration with Events

The system listens to these events:

| Event | Achievement Types Affected |
|-------|---------------------------|
| `EnemyDestroyedDefinition` | KillEnemies |
| `WaveCompleted` | CompleteWaves, ReachDifficulty |
| `RoundCompleted` | CompleteRounds |
| `CurrencyEarned` | EarnCurrency |
| `CurrencySpent` | SpendCurrency |
| `BulletFired` | ShootProjectiles |

**Future Events:**
- `TurretUnlocked` → UnlockTurret achievements
- `ProjectileUnlocked` → UnlockProjectile achievements
- `ProjectileUpgraded` → UpgradeProjectile achievements

---

## API Reference

### AchievementManager

```csharp
// Get all achievements
IReadOnlyList<AchievementRuntime> achievements = AchievementManager.Instance.GetAllAchievements();

// Get specific achievement
AchievementRuntime achievement = AchievementManager.Instance.GetAchievement("ACH_ENEMY_SLAYER");

// Check progress
float progress = achievement.Current;
int tiersComplete = achievement.HighestTierCompleted;
bool isFullyComplete = achievement.IsComplete;

// Get current tier info
AchievementTier currentTier = achievement.GetCurrentTier();
AchievementTier nextTier = achievement.GetNextUncompletedTier();
float progressToNext = achievement.GetProgressToNextTier(); // 0.0 to 1.0
```

### AchievementRuntime Properties

```csharp
float Current // Current progress amount
int HighestTierCompleted // Highest completed tier index (-1 if none)
bool IsComplete // True if all tiers completed
DateTime LastUpdatedUtc // Last update timestamp
```

---

## Daily/Weekly Objectives Integration

Achievements complement the existing Daily/Weekly Objective system:

| Feature | Objectives | Achievements |
|---------|-----------|--------------|
| **Duration** | Time-limited (6-hour slots, daily/weekly) | Permanent |
| **Progress** | Resets on new assignment | Cumulative forever |
| **Rewards** | Single reward on completion | Multiple rewards per tier |
| **Tiers** | Single goal | Multi-tiered milestones |
| **Purpose** | Short-term engagement | Long-term progression |

**Best Practice:** Use achievements for long-term goals that support objectives.

Example:
- **Objective:** "Kill 50 enemies today" (1 day)
- **Achievement:** "Enemy Slayer" (10 / 100 / 1000 / 10000 kills, permanent)

---

## UI Integration (Future)

**Recommended UI Flow:**

1. **Achievement List Screen**
   - Show all achievements (unlocked + locked)
   - Display progress bars per tier
   - Show current tier and next tier
   - Highlight newly completed tiers

2. **Achievement Detail Screen**
   - Show all tiers with checkmarks
   - Display rewards for each tier
   - Show current progress toward next tier
   - Show filters/criteria

3. **Notification on Completion**
   - Pop-up when tier is completed
   - Show tier name and rewards
   - Animate rewards being granted

---

## Performance Considerations

- Dictionary lookups for O(1) achievement retrieval
- Progress only checked for relevant achievement types per event
- Completed achievements skip progress checks
- Save only triggered after progress changes
- Minimal per-frame overhead

---

## Best Practices

### Design Guidelines

1. **Tier Spacing:** Use exponential growth (10, 100, 1000, 10000)
2. **Reward Balance:** Higher tiers should have significantly better rewards
3. **Unique Unlocks:** Gate important unlocks behind high-tier achievements
4. **Categories:** Keep achievements organized by category
5. **Hidden Achievements:** Use `isHidden` for spoiler-heavy achievements

### Balancing

- **Bronze Tier:** Achievable in first session
- **Silver Tier:** Requires several sessions
- **Gold Tier:** Medium-term goal (weeks)
- **Platinum/Diamond:** Long-term goal (months)

### Naming Conventions

- **ID:** `ACH_<CATEGORY>_<NAME>` (e.g., `ACH_COMBAT_BOSS_HUNTER`)
- **Display Name:** User-friendly (e.g., "Boss Hunter")
- **Tier Names:** Consistent across achievements (Bronze/Silver/Gold/Platinum)

---

## Troubleshooting

### Achievements not tracking progress

1. Check AchievementManager is in scene
2. Verify achievement assets are in `Resources/Data/Achievements/`
3. Check Console for initialization messages
4. Ensure events are being fired (check EventManager)

### Rewards not granted

1. Verify reward configuration in achievement tiers
2. Check PlayerManager and SaveManager are initialized
3. Look for reward grant messages in Console
4. Check PlayerData after tier completion

### Progress resets on reload

1. Ensure SaveManager.SaveGame() is called after progress
2. Check PlayerData.achievementProgress is populated
3. Verify save file persistence

---

## Future Enhancements

### Planned Features

1. **Achievement Mastery:** Bonus rewards for completing all achievements in a category
2. **Rare/Epic Tiers:** Higher tier levels with exclusive rewards
3. **Seasonal Achievements:** Limited-time achievements with special rewards
4. **Achievement Chains:** Prerequisite achievements (complete X to unlock Y)
5. **Leaderboards:** Compare achievement progress with other players
6. **Achievement Points:** Meta-currency earned from completions

### Integration Opportunities

- **Skill System:** Achievements grant skill points or unlock skills
- **Tower Customization:** Achievements unlock tower skins/effects
- **Prestige System:** Reset achievements for bonus multipliers

---

## Migration from Objectives

Achievements are complementary to Objectives, not a replacement:

| Use Objectives For | Use Achievements For |
|-------------------|---------------------|
| Daily/weekly tasks | Permanent milestones |
| Time-limited goals | Cumulative progress |
| Variety/rotation | Fixed long-term goals |
| Short-term engagement | Long-term retention |

---

## Credits

**System Design:** Multi-tiered stackable achievements with modular rewards  
**Implementation Pattern:** Based on DailyObjectiveManager and ProjectileUnlockManager  
**Event Integration:** Existing EventManager system  

---

**Last Updated:** 2024  
**System Version:** 1.0  
**Status:** Production Ready
