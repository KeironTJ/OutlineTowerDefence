# Projectile Unlock & Upgrade Examples

This document provides practical examples for configuring projectile unlocks and upgrades.

## Quick Reference

### Unlock Methods Summary

| Method | Best For | Example |
|--------|----------|---------|
| Grant by Default | Starter projectiles | Standard Bullet |
| Wave Requirement | Progressive unlocks | Explosive at Wave 10 |
| Currency Purchase | Optional upgrades | Premium projectiles |
| Prerequisites | Unlock chains | Advanced → Expert |
| Combined | Late-game content | All requirements |

### Upgrade Tiers

| Tier | Max Level | Base Cost | Use Case |
|------|-----------|-----------|----------|
| Basic | 3 | 10-20 | Common projectiles |
| Standard | 5 | 30-50 | Balanced projectiles |
| Advanced | 7 | 75-100 | Rare projectiles |
| Premium | 10 | 150-200 | Endgame projectiles |

---

## Example Configurations

### Example 1: Standard Bullet (Starter)

**ProjectileDefinition:**
```
ID: STD_BULLET
Name: Standard Bullet
Type: Standard
Traits: None

Damage Multiplier: 1.0
Speed Multiplier: 1.0

Max Upgrade Level: 3
Base Upgrade Cost: 10
Damage Per Level: 5%
Speed Per Level: 3%

Benefits: "Reliable, balanced performance"
Tradeoffs: "No special effects"
Overall Rating: 0
```

**ProjectileUnlockDefinition:**
```
Projectile ID: STD_BULLET
Grant By Default: true
Cost: 0
```

**Progression:**
- Level 0: 1.0x damage, 1.0x speed (START)
- Level 1: 1.05x damage, 1.03x speed (10 Prisms)
- Level 2: 1.10x damage, 1.06x speed (20 Prisms)
- Level 3: 1.15x damage, 1.09x speed (30 Prisms)
- **Total Cost: 60 Prisms**

---

### Example 2: Explosive Bolt (Mid-Game)

**ProjectileDefinition:**
```
ID: EXPLOSIVE_BOLT
Name: Explosive Bolt
Type: Bolt
Traits: Explosive

Damage Multiplier: 1.3
Speed Multiplier: 0.85
Explosion Radius: 2.5
Explosion Damage Multiplier: 0.6

Max Upgrade Level: 5
Base Upgrade Cost: 40
Damage Per Level: 8%
Speed Per Level: 4%

Benefits: "High AoE damage, effective against groups"
Tradeoffs: "Slower projectile, higher cost"
Overall Rating: 1.2
```

**ProjectileUnlockDefinition:**
```
Projectile ID: EXPLOSIVE_BOLT
Required Highest Wave: 10
Cost Cores: 500
Cost Prisms: 50
Locked Hint: "Reach Wave 10 and spend 500 Cores + 50 Prisms"
```

**Progression:**
- Level 0: 1.30x damage, 0.85x speed (UNLOCK: Wave 10)
- Level 1: 1.40x damage, 0.88x speed (40 Prisms)
- Level 2: 1.51x damage, 0.92x speed (80 Prisms)
- Level 3: 1.61x damage, 0.95x speed (120 Prisms)
- Level 4: 1.72x damage, 0.99x speed (160 Prisms)
- Level 5: 1.82x damage, 1.02x speed (200 Prisms)
- **Total Unlock: 500 Cores + 50 Prisms**
- **Total Upgrade: 600 Prisms**

---

### Example 3: Piercing Shard (Specialty)

**ProjectileDefinition:**
```
ID: PIERCING_SHARD
Name: Piercing Shard
Type: Shard
Traits: Piercing | Penetrate

Damage Multiplier: 0.9
Speed Multiplier: 1.2
Max Penetrations: 3
Piercing Damage Percent: 15
Piercing Duration: 3
Piercing Tick Rate: 1

Max Upgrade Level: 5
Base Upgrade Cost: 35
Damage Per Level: 6%
Speed Per Level: 3%

Benefits: "Passes through enemies, DoT effect"
Tradeoffs: "Lower initial damage"
Overall Rating: 0.8
```

**ProjectileUnlockDefinition:**
```
Projectile ID: PIERCING_SHARD
Required Highest Wave: 15
Prerequisite Projectile IDs: [STD_BULLET, EXPLOSIVE_BOLT]
Cost Cores: 750
Cost Prisms: 100
Locked Hint: "Requires Wave 15 and both Standard + Explosive unlocked"
```

**Why This Configuration:**
- Requires player to have tried basic projectiles first
- Wave gate ensures mid-game unlock
- Specialty projectile for specific situations

---

### Example 4: Homing Missile (Advanced)

**ProjectileDefinition:**
```
ID: HOMING_MISSILE
Name: Homing Missile
Type: Missile
Traits: Homing

Damage Multiplier: 1.5
Speed Multiplier: 0.9
Homing Turn Rate: 180

Max Upgrade Level: 7
Base Upgrade Cost: 60
Damage Per Level: 7%
Speed Per Level: 5%

Benefits: "Guaranteed hits, excellent for mobile enemies"
Tradeoffs: "Expensive, slower initial speed"
Overall Rating: 1.5
```

**ProjectileUnlockDefinition:**
```
Projectile ID: HOMING_MISSILE
Required Highest Wave: 20
Required Max Difficulty: 3
Cost Cores: 1500
Cost Prisms: 200
Locked Hint: "Master Difficulty 3 and reach Wave 20"
```

**Progression Strategy:**
- High base damage makes it immediately useful
- Extended upgrade levels for long-term investment
- Expensive but powerful for dedicated players

---

### Example 5: Chain Lightning (Premium)

**ProjectileDefinition:**
```
ID: CHAIN_LIGHTNING
Name: Chain Lightning
Type: Energy
Traits: Chain | Slow

Damage Multiplier: 1.2
Speed Multiplier: 1.5
Max Chain Targets: 4
Chain Range: 3.0
Chain Damage Multiplier: 0.85
Slow Multiplier: 0.6
Slow Duration: 2

Max Upgrade Level: 10
Base Upgrade Cost: 100
Damage Per Level: 10%
Speed Per Level: 6%

Benefits: "Chains to multiple enemies, slows targets"
Tradeoffs: "High cost, damage falloff per chain"
Overall Rating: 1.8
```

**ProjectileUnlockDefinition:**
```
Projectile ID: CHAIN_LIGHTNING
Required Highest Wave: 30
Required Max Difficulty: 5
Prerequisite Projectile IDs: [HOMING_MISSILE, PIERCING_SHARD]
Cost Cores: 3000
Cost Prisms: 500
Cost Loops: 10
Locked Hint: "Ultimate projectile - complete Difficulty 5"
```

**Endgame Design:**
- Maximum upgrade levels for long-term progression
- Multiple prerequisites ensure player experience
- Loops cost adds premium feel
- Perfect for endgame content

---

### Example 6: Fragmentation Bomb (Burst Damage)

**ProjectileDefinition:**
```
ID: FRAG_BOMB
Name: Fragmentation Bomb
Type: Shell
Traits: Explosive | IncFragment

Damage Multiplier: 2.0
Speed Multiplier: 0.6
Explosion Radius: 4.0
Explosion Damage Multiplier: 0.8
Reward Multiplier: 1.3

Max Upgrade Level: 5
Base Upgrade Cost: 75
Damage Per Level: 12%
Speed Per Level: 8%

Benefits: "Massive AoE damage, increased fragment drops"
Tradeoffs: "Very slow projectile, hard to aim"
Overall Rating: 1.4
```

**ProjectileUnlockDefinition:**
```
Projectile ID: FRAG_BOMB
Required Highest Wave: 25
Cost Cores: 2000
Cost Prisms: 300
Locked Hint: "Unlock with 2000 Cores + 300 Prisms at Wave 25"
```

**Design Philosophy:**
- High risk, high reward
- Slow speed requires player skill
- Increased fragments encourage use
- Great for farming

---

## Progression Trees

### Tree 1: Damage Focus

```
STD_BULLET (Start)
    ↓
EXPLOSIVE_BOLT (Wave 10)
    ↓
FRAG_BOMB (Wave 25)
```

**Theme:** Increasing AoE and burst damage

### Tree 2: Utility Focus

```
STD_BULLET (Start)
    ↓
PIERCING_SHARD (Wave 15)
    ↓
CHAIN_LIGHTNING (Wave 30)
```

**Theme:** Control and multi-target

### Tree 3: Precision Focus

```
STD_BULLET (Start)
    ↓
HOMING_MISSILE (Wave 20)
```

**Theme:** Accuracy and consistency

---

## Balancing Guidelines

### Early Game (Waves 1-10)
- **Unlocks:** 1-2 projectiles
- **Upgrade Cost:** 10-30 Prisms per level
- **Focus:** Learning mechanics

### Mid Game (Waves 11-20)
- **Unlocks:** 3-5 projectiles
- **Upgrade Cost:** 40-75 Prisms per level
- **Focus:** Specialization

### Late Game (Waves 21-30)
- **Unlocks:** 6-8 projectiles
- **Upgrade Cost:** 100-150 Prisms per level
- **Focus:** Optimization

### Endgame (Waves 31+)
- **Unlocks:** 9+ projectiles
- **Upgrade Cost:** 200+ Prisms per level
- **Focus:** Perfection

---

## Testing Checklist

When creating new projectile configs, test:

- [ ] Base stats feel balanced
- [ ] Unlock requirements are achievable
- [ ] Currency costs are proportional to power
- [ ] Upgrade progression feels meaningful
- [ ] Benefits justify tradeoffs
- [ ] Visual clarity (players understand what it does)
- [ ] Performance (no lag with many projectiles)
- [ ] Save/load persistence
- [ ] UI displays correctly

---

## Common Pitfalls

### ❌ DON'T:
- Make starter projectiles too weak (frustrating)
- Lock essential mechanics behind high waves
- Create useless projectiles (no one will use)
- Ignore tradeoffs (power creep)
- Make upgrades too expensive (grind)

### ✅ DO:
- Provide clear progression paths
- Offer meaningful choices
- Balance power with accessibility
- Test with real players
- Iterate based on feedback

---

**Remember:** The best projectile system offers diverse, balanced options that create interesting strategic choices!
