# Example Projectile Definitions

This file contains example configurations for various projectile types. Use these as templates when creating your own ProjectileDefinition assets.

## Standard Bullet (No Traits)
```
ID: STD_BULLET
Name: Standard Bullet
Type: Standard
Traits: None

Damage Multiplier: 1.0
Speed Multiplier: 1.0

Unlock Wave: 1
```

## Piercing Shard (Damage Over Time)
```
ID: PIERCING_SHARD
Name: Piercing Shard
Type: Shard
Traits: Piercing

Damage Multiplier: 0.8
Speed Multiplier: 1.2

Piercing Damage Percent: 10
Piercing Duration: 3.0
Piercing Tick Rate: 1.0

Description: High-speed shard that deals damage over time
Unlock Wave: 3
```

## Explosive Missile (Area Damage)
```
ID: EXPLOSIVE_MISSILE
Name: Explosive Missile
Type: Missile
Traits: Explosive

Damage Multiplier: 1.2
Speed Multiplier: 0.8

Explosion Radius: 2.0
Explosion Damage Multiplier: 0.6

Description: Slow but powerful missile that damages all enemies in blast radius
Unlock Wave: 5
```

## Penetrating Bolt (Passes Through Enemies)
```
ID: PENETRATING_BOLT
Name: Penetrating Bolt
Type: Bolt
Traits: Penetrate

Damage Multiplier: 0.9
Speed Multiplier: 1.0

Max Penetrations: 3 (0 = infinite)

Description: Passes through up to 3 enemies
Unlock Wave: 4
```

## Slowing Energy Blast
```
ID: SLOW_ENERGY
Name: Slowing Energy
Type: Energy
Traits: Slow

Damage Multiplier: 0.7
Speed Multiplier: 1.1

Slow Multiplier: 0.5 (50% slower)
Slow Duration: 2.5

Description: Reduces enemy movement speed by 50% for 2.5 seconds
Unlock Wave: 6
```

## Chain Lightning
```
ID: CHAIN_LIGHTNING
Name: Chain Lightning
Type: Energy
Traits: Chain

Damage Multiplier: 0.8
Speed Multiplier: 1.5

Max Chain Targets: 4
Chain Range: 2.5
Chain Damage Multiplier: 0.8 (20% reduction per jump)

Description: Jumps to nearby enemies, dealing reduced damage each jump
Unlock Wave: 8
```

## Homing Missile
```
ID: HOMING_MISSILE
Name: Homing Missile
Type: Missile
Traits: Homing

Damage Multiplier: 1.0
Speed Multiplier: 0.9

Homing Turn Rate: 180 (degrees per second)

Description: Tracks target after firing
Unlock Wave: 7
```

## Explosive Penetrating Shell (Combined Traits)
```
ID: EXPLOSIVE_PENETRATING_SHELL
Name: Explosive Penetrating Shell
Type: Shell
Traits: Explosive | Penetrate

Damage Multiplier: 1.1
Speed Multiplier: 0.7

Max Penetrations: 2
Explosion Radius: 1.5
Explosion Damage Multiplier: 0.5

Description: Penetrates enemies while exploding on each hit
Unlock Wave: 10
```

## Fragment Multiplier Plasma
```
ID: FRAGMENT_PLASMA
Name: Fragment Plasma
Type: Plasma
Traits: IncFragment

Damage Multiplier: 0.9
Speed Multiplier: 1.1

Reward Multiplier: 1.5 (50% more fragments)

Description: Enemies killed by this projectile drop 50% more fragments
Unlock Wave: 12
```

## Core Harvester Bolt
```
ID: CORE_BOLT
Name: Core Harvester Bolt
Type: Bolt
Traits: IncoreCores

Damage Multiplier: 1.0
Speed Multiplier: 1.0

Reward Multiplier: 2.0 (100% more cores)

Description: Enemies killed by this projectile drop twice as many cores
Unlock Wave: 15
```

## Ultimate Multi-Trait Projectile
```
ID: ULTIMATE_PROJECTILE
Name: Chaos Orb
Type: Energy
Traits: Explosive | Chain | Slow | Homing

Damage Multiplier: 1.0
Speed Multiplier: 0.8

Explosion Radius: 2.0
Explosion Damage Multiplier: 0.5
Max Chain Targets: 3
Chain Range: 2.0
Chain Damage Multiplier: 0.85
Slow Multiplier: 0.6
Slow Duration: 2.0
Homing Turn Rate: 120

Description: Combines multiple powerful traits for maximum destruction
Unlock Wave: 20
```

## Turret-Specific Examples

### For Standard Turret (ProjectileType.Standard only)
- STD_BULLET
- EXPLOSIVE_STANDARD (Standard + Explosive)
- PIERCING_STANDARD (Standard + Piercing)

### For Microshard Blaster (ProjectileType.Shard only)
- PIERCING_SHARD
- CHAIN_SHARD (Shard + Chain)
- FRAGMENT_SHARD (Shard + IncFragment)

### For Heavy Cannon (ProjectileType.Shell or ProjectileType.Missile)
- EXPLOSIVE_MISSILE
- EXPLOSIVE_PENETRATING_SHELL
- CLUSTER_SHELL (Shell + Explosive + Chain)

## Notes on Balancing

- **Damage Multiplier**: Reduce for projectiles with strong utility traits
- **Speed Multiplier**: Homing projectiles can be slower since they track
- **Trait Combinations**: Each additional trait should reduce base damage by ~10-15%
- **Unlock Waves**: Gate powerful combinations behind progression
- **Reward Multipliers**: Should require trade-offs (lower damage, slower speed)

## Creating New Combinations

When creating new projectile combinations:

1. Start with a base type that matches your turret
2. Add 1-3 complementary traits
3. Adjust damage/speed multipliers for balance
4. Set trait parameters based on intended use
5. Test in-game and iterate

Example thought process:
- Want: Shard that slows and chains
- Type: Shard (for Microshard Blaster)
- Traits: Slow | Chain
- Damage: 0.7x (two strong utility traits)
- Speed: 1.2x (shard is fast)
- Result: Fast spreading slow effect
