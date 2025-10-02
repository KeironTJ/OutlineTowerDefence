# Projectile System - Quick Start Guide

**Need to get started fast? Follow these steps.**

---

## 1. Scene Setup (2 minutes)

### Add Required Managers

1. Open your main scene
2. Create empty GameObjects and add these components:
   - `ProjectileDefinitionManager` (loads projectile assets)
   - `ProjectileUnlockManager` (manages unlock logic)

Both managers auto-load assets from Resources folder.

---

## 2. Create Your First Projectile (3 minutes)

### Step 1: Create Definition
1. Right-click in Project → Create → **Outline** → **ProjectileDefinition**
2. Name it: `StandardBullet`
3. Configure:
   ```
   ID: STD_BULLET
   Name: Standard Bullet
   Type: Standard
   Traits: None
   Damage Multiplier: 1.0
   Speed Multiplier: 1.0
   Max Upgrade Level: 3
   Base Upgrade Cost: 10
   ```
4. Assign your bullet prefab (must have Bullet component)
5. Save to: `Resources/Data/Projectiles/`

### Step 2: Create Unlock Definition
1. Right-click in Project → Create → **Game/Unlocks** → **Projectile Unlock**
2. Name it: `StandardBulletUnlock`
3. Configure:
   ```
   Projectile ID: STD_BULLET
   Grant By Default: true (checked)
   Cost Cores: 0
   ```
4. Save to: `Resources/Data/ProjectileUnlocks/`

---

## 3. Use in Code (1 minute)

### Basic Operations

```csharp
// Check if unlocked
bool unlocked = PlayerManager.main.IsProjectileUnlocked("STD_BULLET");

// Unlock projectile
ProjectileUnlockManager.Instance.TryUnlock(
    PlayerManager.main, "EXPLOSIVE_BOLT", out string reason);

// Upgrade projectile  
PlayerManager.main.TryUpgradeProjectile("STD_BULLET", out string reason);

// Get upgrade level
int level = PlayerManager.main.GetProjectileUpgradeLevel("STD_BULLET");

// Assign to turret slot
PlayerManager.main.SetSelectedProjectileForSlot(0, "STD_BULLET");
```

---

## 4. Test (1 minute)

1. Play scene
2. Check console for manager initialization
3. Verify turret fires projectile
4. Test unlock/upgrade via code or debug UI

---

## Common Use Cases

### Create Explosive Projectile
```
1. Create ProjectileDefinition
   - ID: EXPLOSIVE_BOLT
   - Type: Bolt
   - Traits: Explosive (select from dropdown)
   - Explosion Radius: 2.5
   - Explosion Damage Multiplier: 0.6

2. Create ProjectileUnlockDefinition
   - Projectile ID: EXPLOSIVE_BOLT
   - Required Highest Wave: 10
   - Cost Cores: 500
```

### Create Upgrade Progression
```
In ProjectileDefinition:
- Max Upgrade Level: 5
- Base Upgrade Cost: 40
- Damage Per Level: 8%
- Speed Per Level: 4%

Results:
- Level 0 → 1: 40 Prisms
- Level 1 → 2: 80 Prisms
- Level 2 → 3: 120 Prisms
- etc.
```

### Create Unlock Chain
```
1. Create "Basic" projectile with Grant By Default: true
2. Create "Advanced" projectile with:
   - Prerequisite Projectile IDs: [Basic]
   - Required Highest Wave: 15
3. Player must unlock Basic before Advanced appears
```

---

## Troubleshooting

### Projectile Not Firing
- ✅ ProjectileDefinitionManager in scene?
- ✅ ProjectileDefinition in Resources/Data/Projectiles/?
- ✅ Bullet prefab has Bullet component?
- ✅ ID matches exactly (case-sensitive)?

### Unlock Not Working
- ✅ ProjectileUnlockManager in scene?
- ✅ ProjectileUnlockDefinition in Resources/Data/ProjectileUnlocks/?
- ✅ Requirements met (wave, currency, prerequisites)?
- ✅ Check console for error messages

### Upgrade Not Applying
- ✅ Check PlayerManager.GetProjectileUpgradeLevel() returns correct value
- ✅ Verify Turret uses projectileDefinitionId (not legacy projectilePrefab)
- ✅ Confirm upgrade parameters set in ProjectileDefinition

---

## Next Steps

- **More Details:** [PROJECTILE_SYSTEM_README.md](PROJECTILE_SYSTEM_README.md)
- **Full Setup:** [PROJECTILE_SYSTEM_SETUP.md](PROJECTILE_SYSTEM_SETUP.md)
- **Unlocks/Upgrades:** [PROJECTILE_UNLOCK_UPGRADE_GUIDE.md](PROJECTILE_UNLOCK_UPGRADE_GUIDE.md)
- **Examples:** [Code/Scripts/Gameplay/Projectile/PROJECTILE_UNLOCK_EXAMPLES.md](Code/Scripts/Gameplay/Projectile/PROJECTILE_UNLOCK_EXAMPLES.md)
- **Future Plans:** [FUTURE_ENHANCEMENTS.md](FUTURE_ENHANCEMENTS.md)

---

## Cheat Sheet

### Unlock Methods
| Method | Use For | Example |
|--------|---------|---------|
| Grant By Default | Starters | STD_BULLET |
| Wave Requirement | Progression | Wave 10 unlock |
| Currency | Optional | 500 Cores |
| Prerequisites | Chains | Advanced requires Basic |

### Currency Guide
| Currency | Use | Example Cost |
|----------|-----|--------------|
| Cores | Unlock | 500-2000 |
| Prisms | Upgrade | 10-50 per level |
| Loops | Premium | 5-20 |

### Trait Quick Ref
| Trait | Effect |
|-------|--------|
| Penetrate | Passes through enemies |
| Piercing | Damage over time |
| Explosive | Area damage |
| Slow | Reduces enemy speed |
| Homing | Tracks target |
| Chain | Jumps to nearby enemies |
| IncoreCores | Bonus cores on kill |
| IncFragment | Bonus fragments on kill |

---

**That's it! You're ready to create diverse, upgradable projectiles.**

For advanced features and detailed explanations, see the full documentation.
