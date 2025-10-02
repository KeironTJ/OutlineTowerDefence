# Achievement Examples - Complete Templates

This document provides complete configuration examples for all achievement types.

---

## Combat Achievements

### Enemy Slayer (Any Enemy)
```yaml
ID: ACH_ENEMY_SLAYER
Display Name: Enemy Eliminator
Description: Destroy enemies to prove your combat prowess
Category: Combat
Type: KillEnemies

Filters:
  Target Definition ID: (empty - any enemy)
  Use Tier Filter: ☐
  Use Family Filter: ☐
  Use Trait Filter: ☐

Tiers:
  [0] Bronze - Kill 10 enemies
    Rewards: 100 Cores
  [1] Silver - Kill 100 enemies
    Rewards: 500 Cores
  [2] Gold - Kill 1000 enemies
    Rewards: 2000 Cores, Unlock CHAIN_BULLET
  [3] Platinum - Kill 10000 enemies
    Rewards: 10000 Cores, 50 Loops
```

### Boss Hunter (Boss Enemies Only)
```yaml
ID: ACH_BOSS_HUNTER
Display Name: Boss Hunter
Description: Defeat powerful bosses to earn legendary rewards
Category: Combat
Type: KillEnemies

Filters:
  Target Definition ID: (empty)
  Use Tier Filter: ✓
  Target Enemy Tier: Boss
  Use Family Filter: ☐
  Use Trait Filter: ☐

Tiers:
  [0] Bronze - Kill 1 boss
    Rewards: 500 Cores
  [1] Silver - Kill 10 bosses
    Rewards: 2500 Cores
  [2] Gold - Kill 50 bosses
    Rewards: 10000 Cores, Unlock BOSS_BUSTER_TURRET
  [3] Platinum - Kill 200 bosses
    Rewards: 50000 Cores, 200 Loops
```

### Armored Slayer (Trait-Specific)
```yaml
ID: ACH_ARMORED_KILLER
Display Name: Armor Breaker
Description: Destroy heavily armored enemies
Category: Combat
Type: KillEnemies

Filters:
  Target Definition ID: (empty)
  Use Tier Filter: ☐
  Use Family Filter: ☐
  Use Trait Filter: ✓
  Target Traits: Armored

Tiers:
  [0] Bronze - Kill 25 armored enemies
    Rewards: 200 Cores
  [1] Silver - Kill 250 armored enemies
    Rewards: 1000 Cores
  [2] Gold - Kill 2500 armored enemies
    Rewards: 5000 Cores, Unlock ARMOR_PIERCING_BULLET
```

### Trigger Happy (Projectiles Fired)
```yaml
ID: ACH_TRIGGER_HAPPY
Display Name: Trigger Happy
Description: Fire projectiles relentlessly
Category: Combat
Type: ShootProjectiles

Tiers:
  [0] Bronze - Fire 1000 projectiles
    Rewards: 100 Cores
  [1] Silver - Fire 10000 projectiles
    Rewards: 500 Cores
  [2] Gold - Fire 100000 projectiles
    Rewards: 2500 Cores
  [3] Platinum - Fire 1000000 projectiles
    Rewards: 15000 Cores, 50 Loops
```

---

## Progression Achievements

### Wave Master (Wave Completion)
```yaml
ID: ACH_WAVE_MASTER
Display Name: Wave Master
Description: Complete waves to demonstrate endurance
Category: Progression
Type: CompleteWaves

Tiers:
  [0] Bronze - Complete 10 waves
    Rewards: 200 Prisms
  [1] Silver - Complete 100 waves
    Rewards: 1000 Prisms
  [2] Gold - Complete 500 waves
    Rewards: 5000 Prisms
  [3] Diamond - Complete 2000 waves
    Rewards: 20000 Prisms, 100 Loops
```

### Round Champion (Round Completion)
```yaml
ID: ACH_ROUND_CHAMPION
Display Name: Round Champion
Description: Complete rounds to prove your consistency
Category: Progression
Type: CompleteRounds

Tiers:
  [0] Bronze - Complete 5 rounds
    Rewards: 500 Cores
  [1] Silver - Complete 25 rounds
    Rewards: 2500 Cores
  [2] Gold - Complete 100 rounds
    Rewards: 10000 Cores, 1000 Prisms
  [3] Platinum - Complete 500 rounds
    Rewards: 50000 Cores, 5000 Prisms, 100 Loops
```

### Difficulty Conqueror (Reach High Waves)
```yaml
ID: ACH_DIFFICULTY_MASTER
Display Name: Difficulty Master
Description: Reach incredible wave milestones
Category: Progression
Type: ReachDifficulty

Tiers:
  [0] Novice - Reach wave 10
    Rewards: 500 Cores
  [1] Intermediate - Reach wave 25
    Rewards: 2000 Cores
  [2] Expert - Reach wave 50
    Rewards: 10000 Cores, Unlock ADVANCED_TURRET
  [3] Master - Reach wave 100
    Rewards: 50000 Cores, Unlock ELITE_PROJECTILE, 200 Loops
```

---

## Economy Achievements

### Core Collector (Earn Cores)
```yaml
ID: ACH_CORE_COLLECTOR
Display Name: Core Collector
Description: Accumulate Cores through gameplay
Category: Economy
Type: EarnCurrency

Currency Type: Cores

Tiers:
  [0] Bronze - Earn 1000 Cores
    Rewards: 500 Prisms
  [1] Silver - Earn 10000 Cores
    Rewards: 2000 Prisms
  [2] Gold - Earn 100000 Cores
    Rewards: 10000 Prisms
  [3] Platinum - Earn 1000000 Cores
    Rewards: 50000 Prisms, 100 Loops
```

### Prism Hoarder (Earn Prisms)
```yaml
ID: ACH_PRISM_HOARDER
Display Name: Prism Hoarder
Description: Gather vast amounts of Prisms
Category: Economy
Type: EarnCurrency

Currency Type: Prisms

Tiers:
  [0] Bronze - Earn 500 Prisms
    Rewards: 1000 Cores
  [1] Silver - Earn 5000 Prisms
    Rewards: 5000 Cores
  [2] Gold - Earn 50000 Prisms
    Rewards: 25000 Cores
  [3] Platinum - Earn 500000 Prisms
    Rewards: 100000 Cores, 200 Loops
```

### Big Spender (Spend Cores)
```yaml
ID: ACH_BIG_SPENDER
Display Name: Big Spender
Description: Invest heavily in upgrades and unlocks
Category: Economy
Type: SpendCurrency

Currency Type: Cores

Tiers:
  [0] Bronze - Spend 500 Cores
    Rewards: 100 Prisms
  [1] Silver - Spend 5000 Cores
    Rewards: 1000 Prisms
  [2] Gold - Spend 50000 Cores
    Rewards: 10000 Prisms
  [3] Platinum - Spend 500000 Cores
    Rewards: 50000 Prisms, 100 Loops
```

### Investment Expert (Spend Prisms)
```yaml
ID: ACH_INVESTMENT_EXPERT
Display Name: Investment Expert
Description: Master the art of projectile upgrades
Category: Economy
Type: SpendCurrency

Currency Type: Prisms

Tiers:
  [0] Bronze - Spend 100 Prisms
    Rewards: 500 Cores
  [1] Silver - Spend 1000 Prisms
    Rewards: 5000 Cores
  [2] Gold - Spend 10000 Prisms
    Rewards: 50000 Cores
  [3] Platinum - Spend 100000 Prisms
    Rewards: 250000 Cores, 150 Loops
```

---

## Mastery Achievements

### Arsenal Expander (Unlock Turrets)
```yaml
ID: ACH_TURRET_COLLECTOR
Display Name: Arsenal Expander
Description: Unlock diverse turret types
Category: Mastery
Type: UnlockTurret

Tiers:
  [0] Bronze - Unlock 3 turrets
    Rewards: 500 Cores
  [1] Silver - Unlock 6 turrets
    Rewards: 2000 Cores
  [2] Gold - Unlock 10 turrets
    Rewards: 10000 Cores, 1000 Prisms
  [3] Platinum - Unlock 15 turrets
    Rewards: 50000 Cores, 5000 Prisms, Unlock MASTER_TURRET
```

### Projectile Master (Unlock Projectiles)
```yaml
ID: ACH_PROJECTILE_MASTER
Display Name: Projectile Master
Description: Collect all projectile types
Category: Mastery
Type: UnlockProjectile

Tiers:
  [0] Bronze - Unlock 5 projectiles
    Rewards: 500 Prisms
  [1] Silver - Unlock 10 projectiles
    Rewards: 2000 Prisms
  [2] Gold - Unlock 20 projectiles
    Rewards: 10000 Prisms, Unlock ULTIMATE_PROJECTILE
  [3] Platinum - Unlock 30 projectiles
    Rewards: 50000 Prisms, 200 Loops
```

### Upgrade Specialist (Upgrade Projectiles)
```yaml
ID: ACH_UPGRADE_SPECIALIST
Display Name: Upgrade Specialist
Description: Master projectile enhancement
Category: Mastery
Type: UpgradeProjectile

Tiers:
  [0] Bronze - Perform 10 upgrades
    Rewards: 200 Prisms
  [1] Silver - Perform 50 upgrades
    Rewards: 1000 Prisms
  [2] Gold - Perform 200 upgrades
    Rewards: 5000 Prisms, 50 Loops
  [3] Platinum - Perform 1000 upgrades
    Rewards: 25000 Prisms, 200 Loops
```

---

## Special/Hidden Achievements

### Perfect Start
```yaml
ID: ACH_PERFECT_START
Display Name: Perfect Start
Description: Complete your first round without taking damage
Category: Mastery
Type: CompleteRounds

Is Hidden: ✓

Tiers:
  [0] Perfect - Complete 1 round
    Rewards: 5000 Cores, 1000 Prisms, 25 Loops
```

### Speed Runner
```yaml
ID: ACH_SPEED_RUNNER
Display Name: Speed Runner
Description: Complete a round in record time
Category: Mastery
Type: CompleteRounds

Is Hidden: ✓

Tiers:
  [0] Fast - Complete 1 round under 5 minutes
    Rewards: 2000 Cores, 500 Prisms
  [1] Faster - Complete 10 rounds under 5 minutes
    Rewards: 10000 Cores, 2500 Prisms
  [2] Fastest - Complete 50 rounds under 5 minutes
    Rewards: 50000 Cores, 10000 Prisms, 100 Loops
```

---

## Naming Conventions

### ID Format
```
ACH_<CATEGORY>_<DESCRIPTIVE_NAME>
```

Examples:
- ACH_COMBAT_BOSS_HUNTER
- ACH_PROGRESSION_WAVE_MASTER
- ACH_ECONOMY_CORE_COLLECTOR
- ACH_MASTERY_PROJECTILE_MASTER

### Tier Names
**Standard Progression:**
- Bronze → Silver → Gold → Platinum → Diamond → Master

**Difficulty Progression:**
- Novice → Intermediate → Advanced → Expert → Master

**Speed Progression:**
- Fast → Faster → Fastest

**Skill Progression:**
- Apprentice → Journeyman → Expert → Master → Legend

---

## Reward Balancing Guidelines

### Bronze Tier (Entry Level)
- 100-500 Cores OR 50-200 Prisms
- Small rewards to encourage progression

### Silver Tier (Regular Play)
- 500-2500 Cores OR 200-1000 Prisms
- 2-5x Bronze rewards

### Gold Tier (Dedicated Play)
- 2000-10000 Cores OR 1000-5000 Prisms
- May include common unlocks
- 2-5x Silver rewards

### Platinum Tier (Long-term Goal)
- 10000-50000 Cores OR 5000-25000 Prisms
- 25-100 Loops
- Rare unlocks (turrets, projectiles)
- 5-10x Gold rewards

### Diamond/Master Tier (Ultimate Goal)
- 50000+ Cores OR 25000+ Prisms
- 100-200 Loops
- Legendary unlocks
- Exclusive rewards

---

## Best Practices

1. **Balance Difficulty**: Lower tiers should be achievable quickly, higher tiers should be long-term goals
2. **Reward Appropriately**: Match rewards to effort required
3. **Use Unlocks Wisely**: Don't gate essential content, but offer meaningful bonuses
4. **Progressive Scaling**: Use 10x scaling between tiers for consistency
5. **Test Thoroughly**: Verify target amounts are reasonable for actual gameplay

---

**For implementation details, see:**
- ACHIEVEMENT_SYSTEM_GUIDE.md - Complete guide
- ACHIEVEMENT_QUICK_START.md - Quick start tutorial
- ACHIEVEMENT_IMPLEMENTATION_SUMMARY.md - Technical details
