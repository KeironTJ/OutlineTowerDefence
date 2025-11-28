# Achievement System

This folder contains all achievement definitions for the game.

## Overview

Achievements provide long-term progression goals with multi-tiered rewards. Each achievement can have multiple tiers (Bronze, Silver, Gold, Platinum, etc.) that stack progressively.

## Files in this Folder

- **ACH_EnemySlayer_Example.asset** - Example combat achievement for killing enemies
- **ACH_WaveMaster_Example.asset** - Example progression achievement for completing waves

## Creating New Achievements

1. Right-click in this folder → **Create → Rewards → Achievement**
2. Configure achievement type, tiers, and rewards
3. See **ACHIEVEMENT_QUICK_START.md** in the root folder for detailed instructions

## Achievement Types

- **KillEnemies** - Track enemy kills (with optional filters)
- **ShootProjectiles** - Track projectiles fired
- **CompleteWaves** - Track waves completed
- **CompleteRounds** - Track rounds completed
- **ReachDifficulty** - Track highest wave reached
- **EarnCurrency** - Track currency earned
- **SpendCurrency** - Track currency spent
- **UnlockTurret** - Track turret unlocks
- **UnlockProjectile** - Track projectile unlocks
- **UpgradeProjectile** - Track projectile upgrades

## Best Practices

### Tier Progression
Use exponential scaling (10x multiplier):
- Tier 1: 10
- Tier 2: 100
- Tier 3: 1,000
- Tier 4: 10,000

### Rewards
- Lower tiers: Currency only
- Mid tiers: Currency + unlocks
- High tiers: Large currency + premium rewards (Loops, rare unlocks)

### Naming
- ID: `ACH_<CATEGORY>_<NAME>` (e.g., `ACH_COMBAT_BOSS_HUNTER`)
- Display Name: User-friendly (e.g., "Boss Hunter")
- Tier Names: Consistent (Bronze, Silver, Gold, Platinum, Diamond)

## Documentation

For complete documentation:
- **ACHIEVEMENT_SYSTEM_GUIDE.md** - Comprehensive guide
- **ACHIEVEMENT_QUICK_START.md** - Quick start tutorial
