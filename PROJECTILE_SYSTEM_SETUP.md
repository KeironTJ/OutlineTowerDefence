# Projectile Traits System - Setup Guide

## Quick Start

The projectile traits system is now integrated into your tower defense game. Follow these steps to start using it.

## 1. Scene Setup

### Add ProjectileDefinitionManager to Scene

1. Create an empty GameObject in your main scene
2. Name it "ProjectileDefinitionManager"
3. Add the `ProjectileDefinitionManager` component
4. The manager will auto-load all ProjectileDefinition assets from `Resources/Data/Projectiles/`

**Optional:** You can manually assign definitions in the inspector if you prefer not to use auto-loading.

### Verify Other Managers

Ensure these managers exist in your scene:
- ✓ PlayerManager
- ✓ TurretDefinitionManager
- ✓ SaveManager
- ✓ SkillService

## 2. Create Your First Projectile Definition

### In Unity Editor:

1. Navigate to `Resources/Data/Projectiles/` in your Project window
2. Right-click → Create → Outline → ProjectileDefinition
3. Name it (e.g., "StandardBullet")

### Configure the Definition:

```
Basic Settings:
- ID: "STD_BULLET" (unique identifier)
- Name: "Standard Bullet"
- Description: "Basic projectile with no special traits"
- Type: Standard
- Traits: None

Prefab:
- Assign your bullet prefab (must have Bullet component)

Stats:
- Damage Multiplier: 1.0
- Speed Multiplier: 1.0

Requirements:
- Unlock Wave: 1
```

### Create More Projectiles:

Follow the examples in `Code/Scripts/Gameplay/Projectile/EXAMPLE_PROJECTILES.md`

## 3. Update Turret Definitions

### For Existing Turrets:

Open your TurretDefinition assets and configure:

```
Projectile Constraints:
- Allowed Projectile Types: [Empty = accepts all]
  OR specify: [Standard] or [Shard] or [Missile], etc.
  
- Default Projectile ID: "STD_BULLET"
```

### Example: Standard Turret
```
Name: Standard Turret
Allowed Types: [Standard]
Default Projectile: "STD_BULLET"
```

### Example: Microshard Blaster
```
Name: Microshard Blaster  
Allowed Types: [Shard]
Default Projectile: "MICROSHARD"
```

## 4. Backward Compatibility

### Existing Turrets Continue to Work

- If a turret has `projectilePrefab` set, it will use that
- If `projectileDefinitionId` is also set, the definition takes priority
- No breaking changes to existing turrets

### Migration Path (Optional):

1. Create ProjectileDefinition for existing bullet prefabs
2. Set `defaultProjectileId` in TurretDefinition
3. Optionally clear `projectilePrefab` field
4. Test that turret still fires correctly

## 5. Player Progression Integration

### Unlock Projectiles via Code:

```csharp
// Unlock a new projectile for the player
PlayerManager.main.UnlockProjectile("EXPLOSIVE_MISSILE");

// Check if unlocked
bool unlocked = PlayerManager.main.IsProjectileUnlocked("EXPLOSIVE_MISSILE");

// Assign to turret slot
PlayerManager.main.SetSelectedProjectileForSlot(0, "EXPLOSIVE_MISSILE");
```

### Default Unlocked Projectiles:

The system starts with `STD_BULLET` unlocked. Modify `PlayerData` constructor to change defaults.

## 6. Runtime Assignment

### Assign Projectile to Turret Instance:

```csharp
Turret turret = GetComponent<Turret>();
turret.SetProjectileDefinition("EXPLOSIVE_BOLT");
```

### Get Current Projectile:

```csharp
string currentProjectileId = turret.GetProjectileDefinitionId();
```

## 7. Creating Trait Projectiles

### Example: Explosive Projectile

```
ID: EXPLOSIVE_BOLT
Type: Bolt
Traits: Explosive

Stats:
- Damage Multiplier: 1.2
- Speed Multiplier: 0.9

Explosive Settings:
- Explosion Radius: 2.0
- Explosion Damage Multiplier: 0.6
```

### Example: Multi-Trait Projectile

```
ID: CHAOS_ORB
Type: Energy
Traits: Explosive | Chain | Slow

Configure all relevant trait parameters in the inspector
```

### Available Traits:

- **None** - No special behavior
- **Penetrate** - Passes through enemies
- **Piercing** - Applies damage over time
- **Explosive** - Area of effect damage
- **Slow** - Reduces enemy movement speed
- **IncoreCores** - Increases core rewards (future)
- **IncFragment** - Increases fragment rewards (future)
- **Homing** - Tracks target after firing
- **Chain** - Chains to nearby enemies

## 8. Testing Your Setup

### Test Checklist:

- [ ] ProjectileDefinitionManager exists in scene
- [ ] Created at least one ProjectileDefinition asset in Resources/Data/Projectiles/
- [ ] TurretDefinition has defaultProjectileId set
- [ ] Turret fires projectile with correct behavior
- [ ] Projectile traits work as expected (test each trait)
- [ ] Player can unlock/select projectiles
- [ ] Turret type constraints work (e.g., Shard turret rejects Standard bullets)

### Debug Mode:

Enable debug logging to see projectile system in action:
- Uncomment Debug.Log statements in Bullet.cs
- Check PlayerManager logs for unlock/selection events
- Verify ProjectileDefinitionManager loads definitions on startup

## 9. Common Issues & Solutions

### Projectile Not Firing

**Check:**
- Is ProjectileDefinitionManager in the scene?
- Does the ProjectileDefinition asset exist in Resources/Data/Projectiles/?
- Is the projectile ID spelled correctly?
- Does the projectile prefab have a Bullet component?

### Traits Not Working

**Check:**
- Is ProjectileDefinition assigned to bullet via SetProjectileDefinition()?
- Are trait parameters configured in the definition?
- For DoT effects, does enemy have necessary components?

### Type Constraint Errors

**Check:**
- TurretDefinition.allowedProjectileTypes includes the projectile's type
- Empty list means all types accepted
- Validation happens in PlayerManager.SetSelectedProjectileForSlot()

### Save/Load Issues

**Check:**
- PlayerData.unlockedProjectileIds serializes correctly
- PlayerData.selectedProjectilesBySlot is List, not Dictionary
- SaveManager persists after projectile changes

## 10. Next Steps

### UI Integration (Future Work):

- Create projectile selection UI panel
- Show unlocked projectiles for each slot
- Display trait icons and descriptions
- Preview projectile stats before assignment

### Additional Traits (Future Work):

- Implement slow effect integration with Enemy movement
- Add visual effects for explosions, chains, etc.
- Status effect system for enemies
- Projectile upgrade system

### Balance & Tuning:

- Adjust damage/speed multipliers
- Tune trait parameters (explosion radius, slow duration, etc.)
- Set appropriate unlock wave requirements
- Test projectile combinations for balance

## 11. Architecture Overview

```
ProjectileDefinitionManager (Singleton)
├── Loads all ProjectileDefinition assets
└── Provides lookup and filtering

TurretDefinition (ScriptableObject)
├── Specifies allowed projectile types
└── Has default projectile ID

Turret (MonoBehaviour)
├── Uses projectileDefinitionId
├── Falls back to projectilePrefab
└── Spawns bullets with traits

Bullet (MonoBehaviour)
├── Applies trait effects on hit
├── Uses ProjectileDefinition parameters
└── Creates effect components

PlayerManager (Singleton)
├── Manages unlocked projectiles
└── Validates type constraints
```

## 12. File Locations

```
Code/Scripts/
├── Data/Definitions/
│   ├── ProjectileDefinition.cs (ScriptableObject)
│   └── TurretDefinition.cs (Updated)
├── Gameplay/Projectile/
│   ├── Bullet.cs (Updated with traits)
│   ├── ProjectileTrait.cs (Enum)
│   ├── ProjectileType.cs (Enum)
│   ├── ProjectileDefinitionManager.cs (Manager)
│   ├── PiercingEffect.cs (Helper)
│   ├── SlowEffect.cs (Helper)
│   ├── ProjectileIntegrationExample.cs (Example)
│   ├── README.md (Documentation)
│   └── EXAMPLE_PROJECTILES.md (Templates)
├── Gameplay/Player/
│   ├── PlayerData.cs (Updated)
│   └── PlayerManager.cs (Updated)
└── Gameplay/Turret/
    └── Turret.cs (Updated)

Resources/Data/Projectiles/
└── [Your ProjectileDefinition assets here]
```

## 13. Support & Reference

- See `Code/Scripts/Gameplay/Projectile/README.md` for detailed API documentation
- See `Code/Scripts/Gameplay/Projectile/EXAMPLE_PROJECTILES.md` for projectile templates
- See `Code/Scripts/Gameplay/Projectile/ProjectileIntegrationExample.cs` for code examples

## Need Help?

Common questions:
- How do I add a new trait? See README.md "Implementing New Traits"
- How do I create custom effects? Extend PiercingEffect/SlowEffect pattern
- How do I balance projectiles? Start with EXAMPLE_PROJECTILES.md values

---

**Version:** 1.0  
**Last Updated:** 2024  
**System:** Modular Projectile Traits
