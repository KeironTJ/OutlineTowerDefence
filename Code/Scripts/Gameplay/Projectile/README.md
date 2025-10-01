# Projectile Traits System

## Overview

The projectile traits system enables modular and extensible projectile behavior in the tower defense game. Projectiles can have multiple traits (using flags) that define their behavior when they hit enemies.

## Architecture

### Core Components

1. **ProjectileTrait** (enum with Flags)
   - `None` - No special traits
   - `Penetrate` - Passes through enemies
   - `Piercing` - Applies damage over time
   - `Explosive` - Area of effect damage
   - `Slow` - Reduces enemy movement speed
   - `IncoreCores` - Increases core rewards on kill
   - `IncFragment` - Increases fragment rewards on kill
   - `Homing` - Tracks target after firing
   - `Chain` - Chains to nearby enemies

2. **ProjectileType** (enum)
   - Used to categorize projectiles: Standard, Shard, Energy, Missile, Bolt, Plasma, Shell
   - Turrets can restrict which projectile types they accept

3. **ProjectileDefinition** (ScriptableObject)
   - Contains all configuration for a projectile
   - Prefab reference
   - Traits and type
   - Trait-specific parameters (explosion radius, slow duration, etc.)
   - Damage and speed multipliers

4. **ProjectileDefinitionManager** (Singleton)
   - Manages all projectile definitions
   - Auto-loads from Resources/Data/Projectiles
   - Provides lookup by ID, type, or trait

5. **Bullet** (MonoBehaviour)
   - Enhanced to support projectile traits
   - Applies trait effects on collision
   - Backward compatible with legacy projectiles

6. **Effect Components**
   - `PiercingEffect` - Handles damage over time
   - `SlowEffect` - Handles movement speed reduction

## Usage

### Creating a New Projectile Definition

1. In Unity, right-click in Project window
2. Create > Outline > ProjectileDefinition
3. Configure the definition:
   ```
   ID: "EXPLOSIVE_BOLT"
   Name: "Explosive Bolt"
   Type: Bolt
   Traits: Explosive | Slow
   Prefab: <Your bullet prefab>
   Explosion Radius: 2.0
   Explosion Damage Multiplier: 0.75
   Slow Multiplier: 0.5
   Slow Duration: 2.0
   ```
4. Save to Resources/Data/Projectiles/ folder

### Assigning Projectiles to Turrets

#### Method 1: Using TurretDefinition (Design-Time)
```csharp
// In TurretDefinition ScriptableObject:
defaultProjectileId = "EXPLOSIVE_BOLT";
allowedProjectileTypes = { ProjectileType.Bolt, ProjectileType.Missile };
```

#### Method 2: Runtime Assignment
```csharp
// Get turret instance
Turret turret = GetComponent<Turret>();

// Set projectile definition
turret.SetProjectileDefinition("EXPLOSIVE_BOLT");
```

#### Method 3: Player Selection (via PlayerManager)
```csharp
// Unlock projectile for player
PlayerManager.main.UnlockProjectile("EXPLOSIVE_BOLT");

// Assign to turret slot
PlayerManager.main.SetSelectedProjectileForSlot(0, "EXPLOSIVE_BOLT");
```

### Implementing New Traits

To add a new trait:

1. Add the trait to `ProjectileTrait` enum:
   ```csharp
   MyNewTrait = 1 << 8
   ```

2. Add trait parameters to `ProjectileDefinition`:
   ```csharp
   [Tooltip("For MyNewTrait: description")]
   public float myNewTraitParameter = 1.0f;
   ```

3. Implement the trait effect in `Bullet.ApplyTraitEffects()`:
   ```csharp
   if (projectileDefinition.HasTrait(ProjectileTrait.MyNewTrait))
   {
       ApplyMyNewTraitEffect(enemy);
   }
   ```

4. Create effect method:
   ```csharp
   private void ApplyMyNewTraitEffect(Enemy enemy)
   {
       // Your trait logic here
   }
   ```

## Trait Combinations

Traits can be combined using bitwise OR:
```csharp
traits = ProjectileTrait.Penetrate | ProjectileTrait.Piercing;
```

This allows for powerful projectile types like:
- Explosive + Slow = AOE damage that slows all hit enemies
- Penetrate + Chain = Passes through and chains to nearby enemies
- Piercing + IncFragment = DoT that increases fragment rewards

## Backward Compatibility

The system maintains backward compatibility:
- Turrets can still use direct `projectilePrefab` reference
- If no `projectileDefinitionId` is set, falls back to prefab
- Projectiles without definitions work as before (no traits)

## Examples

### Standard Bullet
```
ID: STD_BULLET
Type: Standard
Traits: None
Damage Multiplier: 1.0
Speed Multiplier: 1.0
```

### Microshard (High fire rate, piercing)
```
ID: MICROSHARD
Type: Shard
Traits: Piercing
Damage Multiplier: 0.8
Piercing Damage: 10%
Piercing Duration: 3s
```

### Explosive Missile
```
ID: EXPLOSIVE_MISSILE
Type: Missile
Traits: Explosive | Homing
Damage Multiplier: 1.5
Explosion Radius: 2.5
Explosion Damage: 50%
Homing Turn Rate: 180°/s
```

## Integration Points

### With Enemy System
- Traits can be checked against enemy traits (future expansion)
- Effects are applied directly to Enemy component

### With Player Progression
- Projectiles can be locked/unlocked via PlayerManager
- Player can select different projectiles per turret slot
- Validation ensures turret accepts projectile type

### With Skill System
- Projectile stats can be modified by skills (future)
- Trait effectiveness could scale with upgrades (future)

## Future Enhancements

- Visual effect system for traits (explosions, chains, etc.)
- Status effect system for enemies (proper slow, stun, etc.)
- Trait synergies and combinations
- Projectile upgrade system
- UI for projectile selection and management
