# Future Enhancements for Projectile System

This document outlines planned features and design considerations for the projectile, turret, and tower systems as mentioned in the original issue.

## Completed Features ✅

### Projectile Traits System
- ✅ All 8 traits implemented with logic (Penetrate, Piercing, Explosive, Slow, IncoreCores, IncFragment, Homing, Chain)
- ✅ Trait parameters fully configurable via ScriptableObjects
- ✅ Modular trait combinations support

### Unlock System
- ✅ ProjectileUnlockDefinition for managing unlock criteria
- ✅ Multiple unlock methods:
  - Wave-based unlocks
  - Difficulty requirements
  - Prerequisite chains
  - Currency purchases (Cores, Prisms, Loops)
  - Manual/default unlocks
- ✅ ProjectileUnlockManager for centralized unlock logic

### Upgrade System
- ✅ Level-based progression per projectile
- ✅ Configurable max levels and costs
- ✅ Percentage-based stat scaling (damage, speed)
- ✅ Prisms as upgrade currency
- ✅ Save/load persistence via PlayerData

### Benefits & Tradeoffs
- ✅ Text fields for describing advantages/disadvantages
- ✅ Overall rating system (-2 to +2)
- ✅ Helps players make informed decisions

### Achievement System
- ✅ AchievementDefinition ScriptableObject for configuring achievements
- ✅ Multi-tiered stackable achievements (Bronze, Silver, Gold, Platinum, etc.)
- ✅ 10 achievement types: KillEnemies, ShootProjectiles, CompleteWaves, CompleteRounds, ReachDifficulty, EarnCurrency, SpendCurrency, UnlockTurret, UnlockProjectile, UpgradeProjectile
- ✅ Achievement categories: Combat, Progression, Economy, Mastery
- ✅ Multiple rewards per tier: Currency, Unlocks (Turrets, Projectiles, Tower Bases), Stat Bonuses
- ✅ Event integration for automatic progress tracking
- ✅ Save/load persistence via PlayerData
- ✅ AchievementManager singleton for centralized management
- ✅ Comprehensive documentation (ACHIEVEMENT_SYSTEM_GUIDE.md)

---

## Future Enhancements 🚀

### 1. Multiple Projectiles Per Turret

**Current State:** One projectile per turret slot  
**Proposed:** Allow turrets to equip multiple projectiles with switching mechanics

#### Implementation Ideas:

**Option A: Ammo System**
```csharp
public class TurretAmmoSystem
{
    public List<ProjectileLoadout> loadouts;
    public int currentLoadoutIndex;
    
    // Cycle through projectile types
    public void SwitchProjectile() { /* ... */ }
}

[Serializable]
public class ProjectileLoadout
{
    public string projectileId;
    public int ammoCount; // -1 for infinite
}
```

**Option B: Type-Based Auto-Selection**
```csharp
// Turret automatically selects best projectile for enemy type
public string SelectProjectileForEnemy(Enemy enemy)
{
    // Logic: explosive for groups, piercing for armor, etc.
}
```

**Option C: Player Toggle**
```
- Hotkey to switch between equipped projectiles
- UI indicator showing active projectile
- Cool-down on switching to prevent abuse
```

**Considerations:**
- UI complexity increases
- Balance: ensure no "always best" combination
- Performance: multiple projectile tracking per turret
- Player skill: reward strategic switching

---

### 2. Enhanced Trait Upgrades

**Current:** Base stats increase per level  
**Proposed:** Trait-specific upgrades

#### Examples:

**Explosive Trait:**
- Level 1: 2.0 radius
- Level 5: 3.0 radius, +20% explosion damage

**Piercing Trait:**
- Level 1: 3 sec duration, 1 tick/sec
- Level 5: 5 sec duration, 2 ticks/sec

**Chain Trait:**
- Level 1: 3 targets, 20% falloff
- Level 5: 5 targets, 10% falloff

#### Implementation:
```csharp
// Add to ProjectileDefinition
[Header("Trait Upgrades")]
public float explosionRadiusPerLevel = 0.2f;
public float piercingDurationPerLevel = 0.4f;
public int chainTargetsPerLevel = 0; // +1 every 2 levels

// Calculation methods
public float GetExplosionRadiusAtLevel(int level) { /* ... */ }
```

---

### 3. Projectile Mastery/Prestige System

**Concept:** After maxing a projectile, unlock additional benefits

#### Features:
- **Mastery Levels:** Beyond max upgrade level
- **Mastery Bonuses:** Unique perks (e.g., "Explosive chain" for Explosive projectiles)
- **Prestige Currency:** Special currency earned from using maxed projectiles
- **Cosmetic Upgrades:** Visual flair for mastered projectiles

#### Example:
```
Explosive Bolt - Mastery 3
├── Bonus: 10% chance to double explosion radius
├── Cost: 500 Mastery Points
└── Requirement: 1000 kills with Explosive Bolt
```

---

### 4. Turret-Projectile Synergies

**Concept:** Certain turret + projectile combos grant bonuses

#### Examples:

**Synergy: Rapid Turret + Piercing Shard**
- Bonus: +20% tick rate on piercing effect
- Theme: Fast turret applies more DoT ticks

**Synergy: Heavy Turret + Explosive Bolt**
- Bonus: +30% explosion radius
- Theme: Slow, powerful turret enhances AoE

**Synergy: Sniper Turret + Homing Missile**
- Bonus: +50% homing turn rate
- Theme: Precision turret improves tracking

#### Implementation:
```csharp
// In TurretDefinition
public List<ProjectileSynergy> synergies;

[Serializable]
public class ProjectileSynergy
{
    public string projectileId;
    public SynergyBonus bonus;
}
```

---

### 5. Tower Base Integration

**Current:** Tower bases exist but limited integration with projectiles  
**Proposed:** Tower bases modify projectile behavior

#### Examples:

**Tower Base: "Core Reactor"**
- Effect: All projectiles gain +15% damage
- Theme: Power boost

**Tower Base: "Velocity Enhancer"**
- Effect: All projectiles gain +25% speed
- Theme: Rate of fire / projectile speed

**Tower Base: "Overcharge Platform"**
- Effect: 5% chance for projectiles to trigger twice
- Theme: RNG power spike

**Tower Base: "Amplifier Array"**
- Effect: Trait effects are 20% stronger (larger explosions, longer DoT, etc.)
- Theme: Enhancement focus

#### Implementation:
```csharp
// Add to TowerBaseData
public List<ProjectileModifier> projectileModifiers;

public class ProjectileModifier
{
    public ModifierType type; // Damage, Speed, TraitPower
    public float multiplier;
}
```

---

### 6. Projectile Crafting System

**Concept:** Combine projectile traits to create custom projectiles

#### Features:
- **Trait Fusion:** Combine two projectiles to merge traits
- **Trait Extraction:** Remove a trait from a projectile
- **Stat Tuning:** Adjust damage/speed balance
- **Rarity System:** Crafted projectiles have quality tiers

#### Example Crafting Recipe:
```
Explosive Bolt + Piercing Shard
├── Result: Explosive Shard
├── Traits: Explosive + Piercing + Penetrate
├── Stats: Average of both parents
└── Cost: 1000 Cores + both projectiles sacrificed (optional)
```

#### Balancing:
- Limit number of traits (max 3?)
- Crafted projectiles more expensive to upgrade
- Unique traits can't be crafted (only found/unlocked)

---

### 7. Conditional Projectile Effects

**Concept:** Traits that trigger under specific conditions

#### New Trait Ideas:

**Conditional Explosive:**
- Only explodes if kills the target
- Higher explosion damage as trade-off

**Adaptive Damage:**
- Increases damage vs high HP enemies
- Decreases damage vs low HP enemies

**Ricochet:**
- Bounces off terrain/borders
- Loses damage per bounce

**Split:**
- On hit, splits into 2-3 smaller projectiles
- Smaller projectiles deal reduced damage

#### Implementation:
```csharp
// New traits
public enum ProjectileTrait
{
    // ... existing traits ...
    Ricochet      = 1 << 8,
    Split         = 1 << 9,
    Conditional   = 1 << 10,
}

// In Bullet.cs
private void ApplyConditionalEffects(Enemy enemy) { /* ... */ }
```

---

### 8. Dynamic Difficulty Scaling

**Concept:** Projectile effectiveness scales with difficulty

#### Features:
- Higher difficulties have stronger enemies
- Projectiles maintain relevance through scaling
- Encourages upgrades at higher difficulties

#### Example:
```
Difficulty 1: Standard Bullet does 100% damage
Difficulty 5: Standard Bullet does 80% damage (enemy armor)
Solution: Upgrade or switch to armor-piercing projectiles
```

---

### 9. Projectile Objectives & Achievements

**Status:** ✅ **Implemented in Achievement System**

**Concept:** Unlock bonuses by completing projectile-specific challenges

The achievement system now supports projectile-specific achievements with the following features:

#### Implementation:
- **AchievementType.ShootProjectiles:** Track total projectiles fired
- **AchievementType.UnlockProjectile:** Track projectile unlocks
- **AchievementType.UpgradeProjectile:** Track projectile upgrades
- **Filters:** Target specific projectile IDs or traits
- **Multi-tier rewards:** Progressive bonuses for milestone achievements

#### Examples Now Possible:

**Achievement: "Trigger Happy"**
- Type: ShootProjectiles
- Tiers: 1000 / 10000 / 100000 / 1000000 shots
- Rewards: Cores and eventual Loop rewards

**Achievement: "Explosive Expert"**
- Type: KillEnemies
- Filter: targetProjectileTrait = Explosive (future enhancement)
- Tiers: 100 / 1000 / 10000 kills
- Rewards: Increased explosion radius (via stat bonus system)

**Achievement: "Projectile Master"**
- Type: UnlockProjectile
- Tiers: Unlock 5 / 10 / 20 / 30 projectiles
- Rewards: Currency and special projectile unlocks

#### Benefits:
- ✅ Encourages variety in projectile usage
- ✅ Provides goals beyond "max everything"
- ✅ Adds replayability
- ✅ Integrates with daily/weekly objective system

#### Future Enhancements:
- Trait-specific kill tracking (e.g., kills with Explosive projectiles)
- Chain length achievements (hit X enemies with one Chain shot)
- Projectile mastery bonuses (permanent stat boosts for achieving milestones)

---

### 10. Visual Upgrade Effects

**Concept:** Projectiles visually evolve as they're upgraded

#### Features:
- **Level 1-3:** Standard appearance
- **Level 4-6:** Glowing effects
- **Level 7-10:** Particle trails, enhanced VFX
- **Mastery:** Unique cosmetic flair

#### Example:
```
Explosive Bolt
├── Level 0: Red bolt
├── Level 3: Red bolt with spark trail
├── Level 5: Orange bolt with fire trail
└── Level 10: Blue-white bolt with lightning trail + larger explosion
```

---

## Design Considerations for Turret System

### Multiple Turret Types on One Base

**Current:** One turret per tower base  
**Proposed:** Combine turret types for hybrid functionality

#### Examples:

**Dual-Mount System:**
- Primary turret (main)
- Secondary turret (support)
- Share range, but independent targeting

**Rotating Specialization:**
- Base turret with swappable heads
- Different heads for different situations
- Cool-down on swapping

---

## Design Considerations for Tower Base System

### Modular Tower Base System

**Concept:** Tower bases as upgrade platforms

#### Features:
- **Base Slots:** Attach modules to tower base
- **Module Types:** 
  - Damage boosters
  - Range extenders
  - Resource generators
  - Special abilities
- **Synergy Bonuses:** Certain module combinations unlock bonuses

---

## Implementation Priority

### Phase 1: Core Enhancements (Next Update)
1. Visual upgrade effects (levels 1-5)
2. Trait-specific upgrade parameters
3. Basic synergy system

### Phase 2: Player Engagement (Medium-term)
4. ✅ **Projectile objectives & achievements** (Implemented via Achievement System)
5. Enhanced tower base integration
6. Dynamic difficulty scaling

### Phase 3: Advanced Features (Long-term)
7. Multiple projectiles per turret
8. Projectile crafting system
9. Mastery/prestige system
10. Conditional projectile effects

### Phase 4: Innovation (Future)
- New trait types
- Community-created projectiles
- Seasonal/event projectiles
- Multiplayer considerations
- Achievement UI integration
- Trait-specific kill tracking for achievements
- Achievement mastery bonuses

---

## Technical Debt & Refactoring

### Areas to Monitor:
- **Performance:** Many projectiles with complex traits
- **Save Data Size:** Tracking upgrades/unlocks/mastery
- **UI Complexity:** Too many options can overwhelm
- **Balance:** Power creep from too many systems

### Recommended Practices:
- Profile performance with 100+ projectiles on screen
- Compress save data (use bit flags where possible)
- Create UX mockups before implementing new UI
- Regular balance passes based on player data

---

## Questions for Designer/Community

Before implementing these features, consider:

1. **Complexity vs Accessibility:** How much is too much?
2. **Currency Balance:** Do we need more currency types?
3. **Player Progression Speed:** How quickly should players unlock everything?
4. **Endgame Content:** What keeps max-level players engaged?
5. **Monetization (if applicable):** Which features are premium vs free?

---

## Conclusion

The projectile system is now robust and extensible. Future enhancements should focus on:
- **Depth:** More strategic choices
- **Variety:** Diverse playstyles
- **Progression:** Long-term goals
- **Polish:** Visual/audio feedback

The foundation is solid. Build features incrementally, test with players, and iterate based on feedback.

---

**Document Version:** 1.0  
**Last Updated:** 2024  
**Status:** Planning Document
