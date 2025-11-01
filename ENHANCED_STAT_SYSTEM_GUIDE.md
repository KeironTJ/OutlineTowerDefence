# Enhanced Tower Stat System - Multiple Stat Bonuses

## Overview
The tower stat system has been enhanced to allow **multiple stat bonuses** from various sources, providing greater flexibility in game design. Each tower component (bases, turrets, projectiles, and chips) can now contribute multiple stats to the tower's performance.

## What Changed

### New Features
1. **Multiple Stat Bonuses**: All definition types can now provide multiple stat bonuses instead of just one
2. **Flexible Stat Sources**: Tower bases, turrets, projectiles, and chips all contribute to final stats
3. **Automatic Integration**: New `TowerLoadoutService` automatically collects stats from player's selected loadout
4. **Backward Compatibility**: Existing single-stat chip definitions continue to work

### New Classes

#### `StatBonus` - Core Bonus Definition
```csharp
[System.Serializable]
public class StatBonus
{
    public StatId targetStat;              // Which stat to modify
    public SkillContributionKind contributionKind;  // How to apply it
    public float value;                    // The bonus value
    public float pipelineScale;            // Scaling factor
    public float pipelineMin/Max;          // Value clamping
}
```

#### `TowerLoadoutService` - Loadout Stat Contributor
A new service that implements `IStatContributor` to provide stats from:
- Selected tower base
- Selected turrets (all 4 slots)
- Selected projectiles (per slot)

## How to Use

### 1. Adding Stat Bonuses to Tower Bases

In Unity Inspector, select your TowerBaseData asset:

```
1. Expand "Stat Bonuses" section
2. Set array size (e.g., 2 for two bonuses)
3. Configure each bonus:
   - Target Stat: e.g., "Damage"
   - Contribution Kind: e.g., "FlatBonus"
   - Value: e.g., 5.0
   - Pipeline Scale: 1.0 (or 0.01 for percentages)
```

**Example**: A reinforced tower base might provide:
- +10 Max Health (FlatBonus)
- +5% Range (Percentage)

### 2. Adding Stat Bonuses to Turrets

In your TurretDefinition asset:

```
1. Expand "Stat Bonuses" section
2. Add bonuses as needed
```

**Example**: A precision turret might provide:
- +15% Accuracy (Percentage)
- +2 Fire Rate (FlatBonus)

### 3. Adding Stat Bonuses to Projectiles

In your ProjectileDefinition asset:

```
1. Expand "Stat Bonuses" section
2. Add bonuses as needed
```

**Example**: An armor-piercing projectile might provide:
- +20% Damage (Percentage)
- -10% Fire Rate (Percentage, negative value)

### 4. Adding Multiple Bonuses to Chips (New)

ChipDefinition now supports both:
- **Legacy mode**: Single `targetStat` and `contributionKind` (still works)
- **New mode**: Multiple bonuses via `statBonuses` array

**Example**: A rare chip might provide:
- +10% Damage (Percentage)
- +5% Fire Rate (Percentage)  
- +2 Range (FlatBonus)

All bonus values automatically scale with chip rarity level.

### 5. Legacy Chip Compatibility

Existing chip definitions using the old single-stat system continue to work:
```csharp
// Old way (still supported):
targetStat = StatId.Damage
contributionKind = SkillContributionKind.Percentage
baseBonus = 10.0
bonusPerRarity = 2.0

// OR new way:
statBonuses[0] = { StatId.Damage, Percentage, 10.0 }
statBonuses[1] = { StatId.FireRate, Percentage, 5.0 }
```

## Architecture

### Stat Collection Flow

```
1. TowerStatPipeline.RebuildImmediate() called
   ↓
2. For each registered IStatContributor:
   - SkillService.Contribute()
   - ChipService.Contribute()
   - TowerLoadoutService.Contribute()  ← NEW
   ↓
3. TowerLoadoutService contributes from:
   - Selected tower base → base.ApplyStatBonuses()
   - Each selected turret → turret.ApplyStatBonuses()
   - Each selected projectile → projectile.ApplyStatBonuses()
   ↓
4. StatCollector accumulates all bonuses
   ↓
5. TowerStatBundle generated with final values
```

### Code Integration Points

#### Triggering Stat Recalculation

Stats automatically recalculate when:
- Skills are upgraded
- Chips are equipped/unequipped/upgraded
- TowerLoadoutService is initialized

**Manual trigger** (when loadout changes):
```csharp
TowerLoadoutService.Instance?.OnLoadoutChanged();
// OR
TowerStatPipeline.SignalDirty();
```

#### Accessing Final Stats

```csharp
var bundle = TowerStatPipeline.Instance.CurrentBundle;
float finalDamage = bundle.Get(StatId.Damage);
```

## Design Guidelines

### When to Use Each Contribution Type

- **Base**: Initial stat value (e.g., tower base provides 100 base health)
- **FlatBonus**: Fixed additive bonus (e.g., +10 damage)
- **Percentage**: Percentage increase (e.g., +15% means value × 1.15)
- **Multiplier**: Direct multiplier (e.g., 2.0 means value × 2.0)

### Calculation Order

Stats are calculated as:
```
Final = (Base + FlatBonus) × Multiplier
```

Where `Percentage` is converted to `Multiplier` (e.g., +15% → 1.15×)

### Balance Considerations

1. **Tower Bases**: Provide foundational bonuses (health, size modifiers)
2. **Turrets**: Provide firing-related bonuses (fire rate, accuracy, rotation)
3. **Projectiles**: Provide damage-related bonuses (damage, penetration, special effects)
4. **Chips**: Provide specialized/rare bonuses (multiple stats for higher rarities)

## Migration Guide

### For Existing Chips

No changes required! Existing chips using `targetStat` will continue to work.

To add multiple bonuses to an existing chip:
1. Keep the legacy fields (for backward compatibility if needed)
2. Add entries to `statBonuses` array
3. The system applies both legacy and new bonuses

### For New Definitions

Use the `statBonuses` array directly for cleaner configuration.

## Examples

### Example 1: Balanced Tower Base
```
Stat Bonuses (Size: 2):
[0] Target Stat: MaxHealth, Kind: Base, Value: 100
[1] Target Stat: Range, Kind: FlatBonus, Value: 2
```

### Example 2: DPS-Focused Turret
```
Stat Bonuses (Size: 2):
[0] Target Stat: FireRate, Kind: Percentage, Value: 20, Scale: 0.01
[1] Target Stat: Damage, Kind: Percentage, Value: 10, Scale: 0.01
```

### Example 3: Legendary Multi-Stat Chip
```
Stat Bonuses (Size: 3):
[0] Target Stat: Damage, Kind: Percentage, Value: 15, Scale: 0.01
[1] Target Stat: FireRate, Kind: Percentage, Value: 10, Scale: 0.01
[2] Target Stat: CritChance, Kind: FlatBonus, Value: 5, Scale: 0.01
```
All values scale with rarity: Level 1 gets 100%, Level 5 gets 200% (with default scaling).

## Testing Your Setup

1. Create test assets with various stat bonuses
2. Equip them in the loadout
3. Check `TowerStatPipeline.Instance.CurrentBundle` in debug mode
4. Verify stats are being applied correctly

## Future Enhancements

Possible future additions:
- Conditional bonuses (only active when certain conditions met)
- Bonus synergies (bonuses that increase when specific combos equipped)
- Dynamic bonus values based on game state
- Bonus visual indicators in UI
