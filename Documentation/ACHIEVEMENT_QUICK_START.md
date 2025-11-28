# Achievement System - Quick Start

## Creating Your First Achievement (5 Minutes)

### Step 1: Create Achievement Asset
1. In Unity Project window, navigate to `Resources/Data/Achievements/`
2. Right-click → **Create → Rewards → Achievement**
3. Name it `ACH_EnemyKiller`

### Step 2: Configure Basic Settings
```
ID: ACH_ENEMY_KILLER
Display Name: Enemy Eliminator
Description: Destroy enemies to prove your combat prowess
Category: Combat
Type: KillEnemies
```

### Step 3: Add Tiers
Create 4 tiers in the Tiers array:

**Tier 0 (Bronze):**
- Tier Level: 1
- Target Amount: 10
- Tier Name: Bronze
- Tier Description: Kill 10 enemies
- Rewards: Add one element
  - Reward Type: Currency
  - Currency Type: Cores
  - Amount: 100

**Tier 1 (Silver):**
- Tier Level: 2
- Target Amount: 100
- Tier Name: Silver
- Tier Description: Kill 100 enemies
- Rewards: Add one element
  - Reward Type: Currency
  - Currency Type: Cores
  - Amount: 500

**Tier 2 (Gold):**
- Tier Level: 3
- Target Amount: 1000
- Tier Name: Gold
- Tier Description: Kill 1000 enemies
- Rewards: Add two elements
  - Element 0:
    - Reward Type: Currency
    - Currency Type: Cores
    - Amount: 2000
  - Element 1:
    - Reward Type: UnlockProjectile
    - Reward ID: CHAIN_BULLET

**Tier 3 (Platinum):**
- Tier Level: 4
- Target Amount: 10000
- Tier Name: Platinum
- Tier Description: Kill 10000 enemies
- Rewards: Add two elements
  - Element 0:
    - Reward Type: Currency
    - Currency Type: Cores
    - Amount: 10000
  - Element 1:
    - Reward Type: Currency
    - Currency Type: Loops
    - Amount: 50

### Step 4: Setup Manager in Scene
1. Create empty GameObject → Name: `AchievementManager`
2. Add Component → `AchievementManager`
3. Leave `All Achievements` empty (auto-loads from Resources)

### Step 5: Test
1. Enter Play Mode
2. Kill 10 enemies → Should see "Bronze" tier completed in Console
3. Kill 100 total enemies → Should see "Silver" tier completed
4. Check PlayerData to verify rewards granted

---

## Common Achievement Templates

### Combat Achievements

#### Boss Hunter
```yaml
Type: KillEnemies
Filters:
  - Use Tier Filter: ✓
  - Target Enemy Tier: Boss
Tiers: 1, 10, 50, 100 bosses
Rewards: Cores, Boss-specific turret unlock
```

#### Sharpshooter
```yaml
Type: ShootProjectiles
Tiers: 1000, 10000, 100000, 1000000 shots
Rewards: Cores, Prisms, Loops
```

### Progression Achievements

#### Wave Master
```yaml
Type: CompleteWaves
Tiers: 10, 100, 500, 2000 waves
Rewards: Prisms, special unlocks
```

#### Difficulty Climber
```yaml
Type: ReachDifficulty
Tiers: Difficulty 2, 3, 4, 5
Rewards: Cores, advanced turrets
```

> Tips:
> - Use **Tools → Achievements → Reach Difficulty Wizard** to auto-generate these difficulty tiers from your `DifficultyProgression` asset.
> - Use **Tools → Achievements → Difficulty Wave Wizard** if you want paired wave milestones once a new difficulty unlocks.

#### Difficulty Wave Master
```yaml
Type: CompleteDifficultyWaves
Difficulty Range: 1-5
Wave Milestones: 10, 20, 50, 100, 500, 1000 (per difficulty)
Rewards: Fragments & Prisms that scale with tier and difficulty
```

> Tip: Use **Tools → Achievements → Difficulty Wave Wizard** to produce staged wave milestones for every difficulty in a couple of clicks. Enable *Create Separate Asset Per Difficulty* when you prefer one achievement per difficulty level.

### Economy Achievements

#### Wealthy Collector
```yaml
Type: EarnCurrency
Currency Type: Cores
Tiers: 1000, 10000, 100000, 1000000
Rewards: Prisms, Loops
```

#### Big Spender
```yaml
Type: SpendCurrency
Currency Type: Prisms
Tiers: 500, 5000, 50000, 500000
Rewards: Cores, special unlocks
```

---

## Achievement Type Reference

| Type | Tracks | Example |
|------|--------|---------|
| KillEnemies | Enemy deaths | Kill 1000 bosses |
| ShootProjectiles | Bullets fired | Fire 1,000,000 shots |
| CompleteWaves | Waves finished | Complete 500 waves |
| CompleteRounds | Rounds finished | Complete 100 rounds |
| ReachDifficulty | Highest difficulty | Unlock difficulty 5 |
| CompleteDifficultyWaves | Highest wave per difficulty | Reach wave 500 on difficulty 3 |
| EarnCurrency | Currency gained | Earn 1,000,000 Cores |
| SpendCurrency | Currency spent | Spend 500,000 Prisms |
| UnlockTurret | Turrets unlocked | Unlock 10 turrets |
| UnlockProjectile | Projectiles unlocked | Unlock 20 projectiles |
| UpgradeProjectile | Projectile upgrades | Upgrade 50 times |

---

## Reward Type Reference

| Type | Configuration | Example |
|------|---------------|---------|
| Currency | Currency Type + Amount | 1000 Cores |
| UnlockTurret | Reward ID | "BOSS_BUSTER" |
| UnlockProjectile | Reward ID | "CHAIN_BULLET" |
| UnlockTowerBase | Reward ID | "0004" |
| StatBonus | Reward ID (future) | "damage_5pct" |

---

## Best Practices

### Tier Progression
Use exponential scaling for tier targets:
- Bronze: 10
- Silver: 100 (10x)
- Gold: 1,000 (10x)
- Platinum: 10,000 (10x)

### Reward Scaling
Higher tiers should have significantly better rewards:
- Bronze: 100 Cores
- Silver: 500 Cores (5x)
- Gold: 2,000 Cores (4x) + Unlock
- Platinum: 10,000 Cores (5x) + Premium Currency

### Unique Unlocks
Gate important unlocks behind high-tier achievements:
- Don't unlock everything via currency
- Make achievements feel rewarding
- Create long-term goals

### Hidden Achievements
Use `Is Hidden` checkbox for spoiler-heavy achievements.

---

## Troubleshooting

**Problem:** Achievements not tracking  
**Solution:** Ensure AchievementManager is in scene and achievements are in `Resources/Data/Achievements/`

**Problem:** Rewards not granted  
**Solution:** Check reward configuration, verify PlayerManager and SaveManager exist

**Problem:** Progress resets  
**Solution:** Verify SaveManager.SaveGame() is called, check PlayerData persistence

---

## Next Steps

1. Create 5-10 achievements for different categories
2. Test all achievement types
3. Balance tier targets and rewards
4. Implement achievement UI (see ACHIEVEMENT_SYSTEM_GUIDE.md)
5. Consider achievement chains (prerequisites)

For complete documentation, see **ACHIEVEMENT_SYSTEM_GUIDE.md**
