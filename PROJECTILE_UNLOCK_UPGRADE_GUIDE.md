# Projectile Unlock & Upgrade System Guide

## Overview

The projectile system now includes comprehensive unlock and upgrade mechanics, allowing for deep progression and customization. This guide covers how to configure and use these systems.

## Table of Contents

1. [Unlock System](#unlock-system)
2. [Upgrade System](#upgrade-system)
3. [Benefits & Tradeoffs](#benefits--tradeoffs)
4. [Setup & Configuration](#setup--configuration)
5. [Code Examples](#code-examples)

---

## Unlock System

### ProjectileUnlockDefinition

Defines how a projectile can be unlocked by the player.

**Fields:**
- `projectileId`: Must match a ProjectileDefinition.id
- `requiredHighestWave`: Minimum wave reached to unlock
- `requiredMaxDifficulty`: Minimum difficulty level reached
- `prerequisiteProjectileIds`: Other projectiles that must be unlocked first
- `grantByDefault`: If true, automatically unlocked for new players
- `costCores`, `costPrisms`, `costLoops`: Currency costs to unlock
- `lockedHint`: Custom UI message (overrides auto-generated)

### Creating Unlock Definitions

**In Unity Editor:**
1. Right-click → Create → Game/Unlocks/Projectile Unlock
2. Configure requirements and costs
3. Save to `Resources/Data/ProjectileUnlocks/`

**Example: Basic Projectile**
```
Projectile ID: STD_BULLET
Grant By Default: true
Cost: 0 (Free)
```

**Example: Advanced Projectile**
```
Projectile ID: EXPLOSIVE_BOLT
Required Highest Wave: 10
Cost Cores: 500
Cost Prisms: 50
Locked Hint: "Unlock at Wave 10"
```

**Example: Chained Unlock**
```
Projectile ID: CHAOS_ORB
Prerequisite Projectiles: [EXPLOSIVE_BOLT, LIGHTNING_BOLT]
Cost Cores: 2000
Cost Prisms: 200
```

### ProjectileUnlockManager

Singleton that manages the unlock system.

**Key Methods:**
- `IsUnlocked(PlayerManager, string projectileId)`: Check unlock status
- `CanUnlock(PlayerManager, string projectileId, out reason, out cost)`: Validate unlock requirements
- `TryUnlock(PlayerManager, string projectileId, out failReason)`: Attempt to unlock

**Auto-Setup:**
- Automatically loads all ProjectileUnlockDefinition assets from Resources
- Grants default projectiles on first run
- Persists unlocks via PlayerManager

---

## Upgrade System

### Upgrade Configuration in ProjectileDefinition

**Fields:**
- `maxUpgradeLevel`: Maximum level (e.g., 5)
- `baseUpgradeCost`: Base cost in Prisms
- `damagePerLevel`: Damage bonus per level (%)
- `speedPerLevel`: Speed bonus per level (%)

**Cost Scaling:**
Cost for upgrading from level N = `baseUpgradeCost * (N + 1)`

Example:
- Level 0 → 1: 10 Prisms (if baseUpgradeCost = 10)
- Level 1 → 2: 20 Prisms
- Level 2 → 3: 30 Prisms
- etc.

**Stat Calculation:**
```
Final Damage Multiplier = baseDamageMultiplier * (1 + damagePerLevel * level / 100)
Final Speed Multiplier = baseSpeedMultiplier * (1 + speedPerLevel * level / 100)
```

### PlayerManager Upgrade API

**GetProjectileUpgradeLevel(string projectileId)**
- Returns: Current upgrade level (0 if not upgraded)

**CanUpgradeProjectile(string projectileId, out reason, out cost)**
- Validates upgrade requirements
- Returns: true if can upgrade, false otherwise
- Outputs: reason (why it can/cannot) and cost (in Prisms)

**TryUpgradeProjectile(string projectileId, out failReason)**
- Attempts to upgrade projectile
- Spends currency and increments level
- Returns: true on success, false on failure

### Example Upgrade Configuration

**Standard Bullet (Conservative)**
```
Max Upgrade Level: 3
Base Upgrade Cost: 10
Damage Per Level: 5%
Speed Per Level: 3%

Result at Level 3:
- Damage: +15% (1.15x)
- Speed: +9% (1.09x)
- Total Cost: 10 + 20 + 30 = 60 Prisms
```

**Explosive Bolt (Aggressive)**
```
Max Upgrade Level: 5
Base Upgrade Cost: 50
Damage Per Level: 10%
Speed Per Level: 5%

Result at Level 5:
- Damage: +50% (1.5x)
- Speed: +25% (1.25x)
- Total Cost: 50 + 100 + 150 + 200 + 250 = 750 Prisms
```

---

## Benefits & Tradeoffs

### Overview

Every projectile should have clear benefits and tradeoffs to create meaningful choices.

**Fields in ProjectileDefinition:**
- `benefits`: Text description (e.g., "High damage, AoE explosion")
- `tradeoffs`: Text description (e.g., "Slower speed, higher cost")
- `overallRating`: -2 (weak) to +2 (strong)

### Design Guidelines

**Balanced Projectile** (rating: 0)
- Clear strengths and weaknesses
- Situationally useful

**Starter Projectile** (rating: -0.5 to 0)
- No major drawbacks
- Lower overall power
- Always available

**Advanced Projectile** (rating: +0.5 to +1.5)
- Strong benefits
- Some tradeoffs
- Requires unlock

**Premium Projectile** (rating: +1.5 to +2)
- Exceptional power
- Expensive to unlock/upgrade
- Late-game option

### Example Configurations

**Standard Bullet**
```
Benefits: "Reliable, balanced performance. No special requirements."
Tradeoffs: "No special effects."
Overall Rating: 0
```

**Explosive Bolt**
```
Benefits: "Area damage hits multiple enemies. High burst potential."
Tradeoffs: "Slower projectile speed. Higher cost per shot."
Overall Rating: +1.0
```

**Piercing Shard**
```
Benefits: "Passes through enemies. DoT effect damages over time."
Tradeoffs: "Lower initial damage. Less effective against single targets."
Overall Rating: +0.5
```

**Chaos Orb**
```
Benefits: "Explosive + Chain + Slow effects. Dominates large groups."
Tradeoffs: "Very expensive. Lower single-target damage."
Overall Rating: +1.8
```

---

## Setup & Configuration

### Scene Setup

1. **Add ProjectileUnlockManager to Scene**
   - Create empty GameObject
   - Add ProjectileUnlockManager component
   - Manager auto-loads definitions from Resources

2. **Verify PlayerManager Exists**
   - Required for unlock/upgrade tracking

3. **Verify ProjectileDefinitionManager Exists**
   - Required for projectile lookups

### Creating a Full Projectile

**Step 1: Create ProjectileDefinition**
```
Assets/Resources/Data/Projectiles/ExplosiveBolt.asset

ID: EXPLOSIVE_BOLT
Name: Explosive Bolt
Type: Bolt
Traits: Explosive
Damage Multiplier: 1.2
Speed Multiplier: 0.8
Explosion Radius: 2.5
Max Upgrade Level: 5
Base Upgrade Cost: 50
Damage Per Level: 8%
Speed Per Level: 4%
Benefits: "Deals AoE damage to nearby enemies"
Tradeoffs: "Slower projectile speed"
Overall Rating: 1.0
```

**Step 2: Create ProjectileUnlockDefinition**
```
Assets/Resources/Data/ProjectileUnlocks/ExplosiveBoltUnlock.asset

Projectile ID: EXPLOSIVE_BOLT
Required Highest Wave: 10
Cost Cores: 500
Cost Prisms: 50
Locked Hint: "Complete Wave 10 to unlock"
```

**Step 3: Assign to Turret**
```csharp
// In game code or via UI
PlayerManager.main.SetSelectedProjectileForSlot(0, "EXPLOSIVE_BOLT");
```

---

## Code Examples

### Unlock Flow

```csharp
// Check if can unlock
string projectileId = "EXPLOSIVE_BOLT";
if (ProjectileUnlockManager.Instance.CanUnlock(
    PlayerManager.main, projectileId, out string reason, out var cost))
{
    Debug.Log($"Can unlock {projectileId} for {cost.ToLabel()}");
    
    // Attempt unlock
    if (ProjectileUnlockManager.Instance.TryUnlock(
        PlayerManager.main, projectileId, out string failReason))
    {
        Debug.Log($"Unlocked {projectileId}!");
    }
    else
    {
        Debug.LogWarning($"Failed: {failReason}");
    }
}
else
{
    Debug.Log($"Cannot unlock: {reason}");
}
```

### Upgrade Flow

```csharp
// Check upgrade eligibility
string projectileId = "EXPLOSIVE_BOLT";
if (PlayerManager.main.CanUpgradeProjectile(
    projectileId, out string reason, out int cost))
{
    Debug.Log($"Can upgrade to level {PlayerManager.main.GetProjectileUpgradeLevel(projectileId) + 1} for {cost} Prisms");
    
    // Attempt upgrade
    if (PlayerManager.main.TryUpgradeProjectile(projectileId, out string failReason))
    {
        int newLevel = PlayerManager.main.GetProjectileUpgradeLevel(projectileId);
        Debug.Log($"Upgraded to level {newLevel}!");
    }
    else
    {
        Debug.LogWarning($"Failed: {failReason}");
    }
}
else
{
    Debug.Log($"Cannot upgrade: {reason}");
}
```

### Query Projectile Stats

```csharp
var projDef = ProjectileDefinitionManager.Instance.GetById("EXPLOSIVE_BOLT");
if (projDef != null)
{
    int level = PlayerManager.main.GetProjectileUpgradeLevel("EXPLOSIVE_BOLT");
    
    float damageMultiplier = projDef.GetDamageMultiplierAtLevel(level);
    float speedMultiplier = projDef.GetSpeedMultiplierAtLevel(level);
    
    Debug.Log($"At level {level}:");
    Debug.Log($"  Damage: {damageMultiplier:F2}x");
    Debug.Log($"  Speed: {speedMultiplier:F2}x");
    Debug.Log($"  Benefits: {projDef.benefits}");
    Debug.Log($"  Tradeoffs: {projDef.tradeoffs}");
}
```

### UI Display Example

```csharp
// Display upgrade info for UI
void DisplayUpgradeInfo(string projectileId)
{
    var projDef = ProjectileDefinitionManager.Instance.GetById(projectileId);
    if (projDef == null) return;
    
    int currentLevel = PlayerManager.main.GetProjectileUpgradeLevel(projectileId);
    
    // Current stats
    float currentDamage = projDef.GetDamageMultiplierAtLevel(currentLevel);
    float currentSpeed = projDef.GetSpeedMultiplierAtLevel(currentLevel);
    
    // Next level stats (if not max)
    if (currentLevel < projDef.maxUpgradeLevel)
    {
        float nextDamage = projDef.GetDamageMultiplierAtLevel(currentLevel + 1);
        float nextSpeed = projDef.GetSpeedMultiplierAtLevel(currentLevel + 1);
        int upgradeCost = projDef.GetUpgradeCost(currentLevel);
        
        Debug.Log($"Current: {currentDamage:F2}x damage, {currentSpeed:F2}x speed");
        Debug.Log($"Next: {nextDamage:F2}x damage, {nextSpeed:F2}x speed");
        Debug.Log($"Cost: {upgradeCost} Prisms");
    }
    else
    {
        Debug.Log($"MAX LEVEL: {currentDamage:F2}x damage, {currentSpeed:F2}x speed");
    }
}
```

---

## Currency Strategy

### Recommended Currency Usage

**Cores (Primary Currency)**
- Unlock new projectiles
- Major progression gates
- Example: 500-2000 Cores per projectile

**Prisms (Secondary Currency)**
- Upgrade projectiles
- Incremental improvements
- Example: 10-50 Prisms per upgrade level

**Loops (Time-Based Currency)**
- Optional: Premium projectile unlocks
- Alternative to grinding
- Example: 5-20 Loops for rare projectiles

**Fragments (Round Currency)**
- Not used for projectiles (in-round only)

### Balancing Tips

1. **Early Game**: Cheap unlocks, low upgrade costs
2. **Mid Game**: Moderate costs, encourage multiple projectiles
3. **Late Game**: Expensive high-tier projectiles, max upgrades as long-term goals
4. **Variety**: Ensure multiple viable projectile choices at each stage

---

## Future Enhancements

### Planned Features
- Visual upgrade effects (glow, particles)
- Trait upgrades (increase explosion radius, etc.)
- Prestige/mastery system
- Projectile combinations (multi-slot turrets)
- Temporary projectile buffs

### Extensibility
The system is designed to support:
- Custom unlock conditions (achievements, objectives)
- Dynamic costs (discounts, events)
- Stat caps and diminishing returns
- Unique upgrade trees per projectile

---

## Troubleshooting

### Projectile Won't Unlock
- Check ProjectileUnlockDefinition exists in Resources/Data/ProjectileUnlocks
- Verify projectileId matches exactly
- Ensure requirements are met (wave, difficulty, prerequisites)
- Check currency availability

### Upgrades Not Applying
- Verify PlayerManager.GetProjectileUpgradeLevel() returns correct value
- Check Turret is using projectileDefinitionId (not legacy projectilePrefab)
- Ensure Bullet.SetProjectileDefinition() is called with upgrade level
- Review ProjectileDefinition upgrade parameters

### Save/Load Issues
- Confirm PlayerData.projectileUpgradeLevels initializes properly
- Check SaveManager is saving after upgrades
- Verify ProjectileUpgradeLevel serialization

---

**Version:** 2.0  
**Last Updated:** 2024  
**System:** Projectile Unlock & Upgrade
