# Projectile System - Quick Reference

## Create New Projectile

1. Unity: Right-click → Create → Outline → ProjectileDefinition
2. Set ID, name, type, traits
3. Assign bullet prefab
4. Configure trait parameters
5. Save to `Resources/Data/Projectiles/`

## Projectile Types

- **Standard** - Basic bullets
- **Shard** - Sharp projectiles
- **Energy** - Laser/energy
- **Missile** - Guided/explosive
- **Bolt** - Heavy projectiles
- **Plasma** - Advanced energy
- **Shell** - Artillery rounds

## Projectile Traits (Flags)

| Trait | Effect | Key Parameters |
|-------|--------|----------------|
| **Penetrate** | Passes through enemies | maxPenetrations |
| **Piercing** | Damage over time | piercingDamagePercent, piercingDuration |
| **Explosive** | Area damage | explosionRadius, explosionDamageMultiplier |
| **Slow** | Reduces movement | slowMultiplier, slowDuration |
| **IncoreCores** | More cores | rewardMultiplier |
| **IncFragment** | More fragments | rewardMultiplier |
| **Homing** | Tracks target | homingTurnRate |
| **Chain** | Jumps to enemies | maxChainTargets, chainRange |

## Code Examples

### Unlock Projectile
```csharp
PlayerManager.main.UnlockProjectile("EXPLOSIVE_BOLT");
```

### Assign to Slot
```csharp
PlayerManager.main.SetSelectedProjectileForSlot(0, "EXPLOSIVE_BOLT");
```

### Direct Turret Assignment
```csharp
turret.SetProjectileDefinition("EXPLOSIVE_BOLT");
```

### Query Projectiles
```csharp
var explosiveProjs = ProjectileDefinitionManager.Instance
    .GetByTrait(ProjectileTrait.Explosive);
```

### Check Compatibility
```csharp
bool canUse = turretDef.AcceptsProjectileType(projDef.projectileType);
```

## Balancing Guidelines

| Trait Count | Damage Multiplier | Speed Multiplier |
|-------------|-------------------|------------------|
| 0 (None) | 1.0 | 1.0 |
| 1 trait | 0.8 - 0.9 | 0.9 - 1.2 |
| 2 traits | 0.7 - 0.8 | 0.8 - 1.0 |
| 3+ traits | 0.6 - 0.7 | 0.7 - 0.9 |

## Scene Setup Checklist

- [ ] ProjectileDefinitionManager in scene
- [ ] ProjectileDefinition assets in Resources/Data/Projectiles/
- [ ] TurretDefinition.defaultProjectileId set
- [ ] Turret prefabs have Bullet component
- [ ] PlayerManager initialized

## Common Patterns

### Standard Bullet (No Traits)
```
Type: Standard, Traits: None
Damage: 1.0, Speed: 1.0
```

### AOE Slow
```
Type: Energy, Traits: Explosive | Slow
Damage: 0.7, Speed: 1.0
Explosion Radius: 2.0, Slow: 50%
```

### Piercing Penetrator
```
Type: Bolt, Traits: Piercing | Penetrate
Damage: 0.8, Speed: 0.9
Pierce Duration: 3s, Max Pen: 3
```

### Chain Lightning
```
Type: Energy, Traits: Chain
Damage: 0.8, Speed: 1.5
Targets: 4, Range: 2.5
```

## File Structure

```
Resources/Data/Projectiles/
├── StandardBullet.asset
├── ExplosiveMissile.asset
├── PiercingShard.asset
└── [Your projectiles]

Scene Hierarchy:
├── ProjectileDefinitionManager
├── TurretDefinitionManager
├── PlayerManager
└── [Other managers]
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Projectile not firing | Check manager in scene, verify ID |
| Traits not working | Verify definition assigned, check parameters |
| Type constraint error | Check turret allowed types |
| Save not persisting | Verify PlayerData serialization |

## Key Classes

- **ProjectileDefinition** - ScriptableObject with all projectile data
- **ProjectileDefinitionManager** - Singleton manager
- **Bullet** - MonoBehaviour with trait logic
- **Turret** - Updated to use definitions
- **PlayerManager** - Handles unlock/selection

## Documentation

- Full API: `Code/Scripts/Gameplay/Projectile/README.md`
- Examples: `Code/Scripts/Gameplay/Projectile/EXAMPLE_PROJECTILES.md`
- Setup: `PROJECTILE_SYSTEM_SETUP.md`
- Code Examples: `ProjectileIntegrationExample.cs`
