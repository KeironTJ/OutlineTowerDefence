# Projectile System Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    PROJECTILE TRAITS SYSTEM                   │
└─────────────────────────────────────────────────────────────┘

┌───────────────────────┐         ┌───────────────────────┐
│   ProjectileTrait     │         │   ProjectileType      │
│      (Flags Enum)     │         │       (Enum)          │
├───────────────────────┤         ├───────────────────────┤
│ - None                │         │ - Standard            │
│ - Penetrate           │         │ - Shard               │
│ - Piercing            │         │ - Energy              │
│ - Explosive           │         │ - Missile             │
│ - Slow                │         │ - Bolt                │
│ - IncoreCores         │         │ - Plasma              │
│ - IncFragment         │         │ - Shell               │
│ - Homing              │         │                       │
│ - Chain               │         │                       │
└───────────────────────┘         └───────────────────────┘
           │                                  │
           └──────────────┬───────────────────┘
                          ↓
           ┌──────────────────────────────┐
           │   ProjectileDefinition       │
           │    (ScriptableObject)        │
           ├──────────────────────────────┤
           │ + id: string                 │
           │ + projectileType: Type       │
           │ + traits: Trait (flags)      │
           │ + projectilePrefab: GameObject│
           │ + damageMultiplier: float    │
           │ + speedMultiplier: float     │
           │ + [Trait Parameters...]      │
           └──────────────────────────────┘
                          │
                          ↓
           ┌──────────────────────────────┐
           │ ProjectileDefinitionManager  │
           │        (Singleton)           │
           ├──────────────────────────────┤
           │ + GetById(id)                │
           │ + GetByType(type)            │
           │ + GetByTrait(trait)          │
           │ + GetAll()                   │
           └──────────────────────────────┘
                          │
                          │ provides definitions to
                          ↓
```

## Component Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                        GAME FLOW                             │
└─────────────────────────────────────────────────────────────┘

┌──────────────┐          ┌──────────────┐          ┌──────────────┐
│ PlayerManager│          │    Turret    │          │    Bullet    │
│  (Singleton) │          │(MonoBehaviour│          │(MonoBehaviour│
├──────────────┤          ├──────────────┤          ├──────────────┤
│ Unlocked IDs │──select→ │projectileDefID──spawn─→ │ Definition   │
│ Selected IDs │          │              │          │ + traits     │
│              │          │ Shoot()      │          │              │
│ Unlock()     │          │ - Get def    │          │ ApplyTraits()│
│ Select()     │          │ - Spawn      │          │ - Penetrate  │
└──────────────┘          │ - Configure  │          │ - Explosive  │
       ↑                  └──────────────┘          │ - Chain...   │
       │                         ↑                  └──────────────┘
       │                         │                         │
       │                  ┌──────────────┐                 │
       │                  │TurretDefinition               │
       │                  │(ScriptableObject              │
       └──────validates───┤ + allowedTypes                │
                          │ + defaultProjectileId         │
                          └──────────────┘                │
                                                           │
                                   ┌───────────────────────┘
                                   │ affects
                                   ↓
                          ┌──────────────┐
                          │    Enemy     │
                          │(MonoBehaviour│
                          ├──────────────┤
                          │ TakeDamage() │
                          │ + DoT effects│
                          │ + Slow       │
                          └──────────────┘
```

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    DATA FLOW                                 │
└─────────────────────────────────────────────────────────────┘

Design Time:                  Runtime:
─────────────                ──────────

[Unity Editor]               [Game Scene]
      │                            │
      ↓                            ↓
┌─────────────┐            ┌─────────────┐
│ Create      │            │ Manager     │
│ Projectile  │  loaded by │ loads defs  │
│ Definition  ├───────────→│ from        │
│ .asset      │            │ Resources/  │
└─────────────┘            └─────────────┘
      │                            │
      │ saved to                   │ provides to
      ↓                            ↓
[Resources/              [Turret selects]
 Data/                         │
 Projectiles/]                 ↓
                        ┌─────────────┐
                        │ Turret.     │
                        │ Shoot()     │
                        └─────────────┘
                               │
                               │ spawns with
                               ↓
                        ┌─────────────┐
                        │ Bullet +    │
                        │ Definition  │
                        └─────────────┘
                               │
                               │ applies to
                               ↓
                        ┌─────────────┐
                        │   Enemy     │
                        └─────────────┘
```

## Player Progression Flow

```
┌─────────────────────────────────────────────────────────────┐
│              PLAYER PROJECTILE PROGRESSION                   │
└─────────────────────────────────────────────────────────────┘

┌──────────────┐
│  New Player  │
│  starts with │
│  "STD_BULLET"│
└──────┬───────┘
       │
       │ gameplay/achievements
       ↓
┌──────────────────────┐
│ Unlock Projectiles   │
│ PlayerManager.       │
│ UnlockProjectile()   │
└──────┬───────────────┘
       │
       ↓
┌──────────────────────┐         ┌──────────────────────┐
│  Unlocked List       │         │  Validation          │
│  - STD_BULLET       │         │  - Type compatible?  │
│  - EXPLOSIVE_BOLT   │◄────────┤  - Turret accepts?   │
│  - PIERCING_SHARD   │   check │  - Unlocked?         │
└──────┬───────────────┘         └──────────────────────┘
       │
       │ player selects
       ↓
┌──────────────────────┐
│ Assign to Slot       │
│ PlayerManager.       │
│ SetSelectedFor...()  │
└──────┬───────────────┘
       │
       ↓
┌──────────────────────┐
│ PlayerData saved     │
│ - unlockedIds[]      │
│ - selectedBySlot[]   │
└──────┬───────────────┘
       │
       │ loaded on game start
       ↓
┌──────────────────────┐
│ Turret uses selected │
│ projectile for slot  │
└──────────────────────┘
```

## Trait Application Flow

```
┌─────────────────────────────────────────────────────────────┐
│                  TRAIT EXECUTION FLOW                        │
└─────────────────────────────────────────────────────────────┘

Bullet hits Enemy
       │
       ↓
┌─────────────────────┐
│ OnCollisionEnter2D  │
└─────┬───────────────┘
      │
      ├──→ Apply base damage
      │
      ├──→ Check for Penetrate trait
      │    ├─ Yes: Continue flying, increment count
      │    └─ No: Destroy bullet
      │
      └──→ ApplyTraitEffects()
           │
           ├──→ Has Piercing?
           │    └─→ Add PiercingEffect component to enemy
           │        ├─ Damage per tick
           │        ├─ Duration
           │        └─ Tick rate
           │
           ├──→ Has Explosive?
           │    └─→ Find enemies in radius
           │        ├─ Apply AoE damage
           │        └─ [Spawn VFX]
           │
           ├──→ Has Slow?
           │    └─→ Add SlowEffect component
           │        ├─ Speed multiplier
           │        └─ Duration
           │
           ├──→ Has Chain?
           │    └─→ Find nearest unchained enemy
           │        ├─ Apply reduced damage
           │        ├─ Repeat for max targets
           │        └─ [Create visual chain]
           │
           └──→ Has Homing?
                └─→ Already handled in FixedUpdate
                    ├─ Rotate toward target
                    └─ Update velocity

```

## Integration Points

```
┌─────────────────────────────────────────────────────────────┐
│              SYSTEM INTEGRATION MAP                          │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│  Projectile System  │
└─────────┬───────────┘
          │
          ├──→ Turret System
          │    ├─ Fires projectiles
          │    ├─ Type constraints
          │    └─ Default projectiles
          │
          ├──→ Player System
          │    ├─ Unlock progression
          │    ├─ Slot selection
          │    └─ Save/Load
          │
          ├──→ Enemy System
          │    ├─ Damage application
          │    ├─ Effect components
          │    └─ [Future: status effects]
          │
          ├──→ Skill System
          │    ├─ Damage multipliers
          │    ├─ Speed multipliers
          │    └─ [Future: trait effectiveness]
          │
          └──→ [Future Integrations]
               ├─ VFX System (explosions, chains)
               ├─ SFX System (trait sounds)
               ├─ UI System (selection, preview)
               ├─ Achievement System
               └─ Analytics System

```

## Class Hierarchy

```
UnityEngine.MonoBehaviour
├── Bullet (enhanced)
│   ├── SetProjectileDefinition()
│   └── ApplyTraitEffects()
│
├── Turret (enhanced)
│   ├── SetProjectileDefinition()
│   └── Shoot() [modified]
│
├── ProjectileDefinitionManager
│   ├── Awake() - auto-load
│   └── Query methods
│
├── PiercingEffect
│   └── Update() - tick damage
│
└── SlowEffect
    └── Update() - track duration

UnityEngine.ScriptableObject
├── ProjectileDefinition
│   └── HasTrait()
│
└── TurretDefinition (enhanced)
    └── AcceptsProjectileType()

[Serializable]
├── PlayerData (enhanced)
│   ├── unlockedProjectileIds
│   └── selectedProjectilesBySlot
│
└── ProjectileSlotAssignment
    ├── slotIndex
    └── projectileId
```

## File Organization

```
OutlineTowerDefence/
├── Code/Scripts/
│   ├── Data/Definitions/
│   │   ├── ProjectileDefinition.cs      [ScriptableObject]
│   │   └── TurretDefinition.cs          [Enhanced]
│   │
│   └── Gameplay/
│       ├── Projectile/
│       │   ├── ProjectileTrait.cs       [Enum - Flags]
│       │   ├── ProjectileType.cs        [Enum]
│       │   ├── ProjectileDefinitionManager.cs [Manager]
│       │   ├── Bullet.cs                [Enhanced]
│       │   ├── PiercingEffect.cs        [Effect]
│       │   ├── SlowEffect.cs            [Effect]
│       │   ├── ProjectileIntegrationExample.cs [Example]
│       │   ├── README.md                [API Docs]
│       │   └── EXAMPLE_PROJECTILES.md   [Templates]
│       │
│       ├── Turret/
│       │   └── Turret.cs                [Enhanced]
│       │
│       └── Player/
│           ├── PlayerData.cs            [Enhanced]
│           └── PlayerManager.cs         [Enhanced]
│
├── Resources/Data/Projectiles/
│   └── [ProjectileDefinition assets]
│
└── [Root Documentation]
    ├── PROJECTILE_SYSTEM_SETUP.md       [Setup Guide]
    ├── PROJECTILE_QUICK_REFERENCE.md    [Quick Ref]
    ├── IMPLEMENTATION_SUMMARY.md        [Summary]
    └── PROJECTILE_SYSTEM_ARCHITECTURE.md [This File]
```

## Trait Combinations Matrix

```
┌────────────┬──────────┬──────────┬──────────┬──────────┐
│  Primary   │ Explosive│ Piercing │  Slow    │  Chain   │
│  Trait     │          │          │          │          │
├────────────┼──────────┼──────────┼──────────┼──────────┤
│ Penetrate  │   AOE    │   DoT    │  Crowd   │ Spread   │
│            │  Pierce  │  Pierce  │ Control  │  Pierce  │
├────────────┼──────────┼──────────┼──────────┼──────────┤
│ Homing     │ Guided   │ Tracking │ Seeking  │ Smart    │
│            │   Bomb   │   Bleed  │   Slow   │  Chain   │
├────────────┼──────────┼──────────┼──────────┼──────────┤
│ Explosive  │    -     │ Lasting  │ Slowing  │ Cascade  │
│            │          │   Blast  │   Blast  │   Blast  │
└────────────┴──────────┴──────────┴──────────┴──────────┘

Legend:
- Penetrate + Explosive = AOE damage on each enemy hit
- Homing + Chain = Seeks and chains intelligently
- Explosive + Slow = Slows all enemies in blast
- Chain + Explosive = Each chain causes mini-explosion
```

## Extension Points

```
New Trait Addition:
─────────────────────

1. Add to ProjectileTrait enum
   └─> ProjectileTrait.cs

2. Add parameters to ProjectileDefinition
   └─> ProjectileDefinition.cs

3. Implement in ApplyTraitEffects()
   └─> Bullet.cs

4. [Optional] Create effect component
   └─> New Effect.cs file

5. Document in examples
   └─> EXAMPLE_PROJECTILES.md


New Projectile Type:
───────────────────

1. Add to ProjectileType enum
   └─> ProjectileType.cs

2. Configure in TurretDefinition
   └─> Set allowedProjectileTypes

3. Create definition asset
   └─> Unity Editor


New Integration:
───────────────

1. Access ProjectileDefinitionManager
2. Query definitions as needed
3. Check traits/types
4. Apply custom logic
```

---

This architecture enables:
- ✅ Modular trait system
- ✅ Type-safe constraints
- ✅ Easy extension
- ✅ Clear separation of concerns
- ✅ Designer-friendly workflow
