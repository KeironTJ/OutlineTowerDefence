# Chips System Guide

## Overview
The Chips system is a card-based progression feature that allows players to gain additional buffs to their skills and gameplay. Chips work on a rarity system where collecting multiple copies of the same chip increases its rarity and bonus effectiveness.

## Key Features

### 1. Chip Definitions (ScriptableObject)
- **Location**: `Code/Scripts/Data/Definitions/ChipDefinition.cs`
- **Create via**: `Assets > Create > Outline > ChipDefinition`

Each chip has:
- **Identity**: ID, name, description, icon
- **Bonus Type**: Attack damage, attack speed, health, fragments boost, etc.
- **Base Bonus**: Starting bonus value at Common rarity
- **Bonus Per Rarity**: How much the bonus increases per rarity level
- **Rarity Progression**: Array defining how many chips needed for each rarity (e.g., [0, 3, 5, 7, 10])
- **Restrictions**: Can change in round, unlock wave requirement

### 2. Rarity System
Chips have 5 rarity levels:
- **Common** (0) - Default when first unlocked
- **Uncommon** (1) - Requires 3 chips
- **Rare** (2) - Requires 5 chips
- **Epic** (3) - Requires 7 chips
- **Legendary** (4) - Requires 10 chips

*Note: These thresholds are customizable per chip via the `chipsNeededForRarity` array*

### 3. Available Bonus Types
The system supports these bonus types (extensible):
- `AttackDamageMultiplier` - Increases attack damage
- `AttackSpeed` - Increases fire rate
- `Health` - Increases tower health
- `HealthRecoverySpeed` - Increases health regeneration
- `FragmentsBoost` - Multiplies fragments gained
- `CriticalChance` - Increases critical hit chance
- `CriticalDamage` - Increases critical damage
- `ProjectileSpeed` - Increases projectile velocity
- `TurretRange` - Increases turret range
- `ExperienceBoost` - Multiplies experience gained

## Player Data Storage

### PlayerData Integration
The following data is stored in `PlayerData`:
```csharp
[Header("Chips System")]
public ChipSystemConfig chipConfig;           // Slot configuration
public List<ChipProgressData> chipProgress;   // Collection progress
public List<ChipSlotData> equippedChips;      // Active loadout
```

### ChipProgressData
Tracks collection and upgrade progress:
- `chipId` - Chip identifier
- `chipCount` - Total chips collected
- `rarityLevel` - Current rarity (0-4)
- `unlocked` - Whether player has unlocked this chip

### ChipSlotData
Tracks equipped chips:
- `slotIndex` - Which slot (0-based)
- `equippedChipId` - ID of equipped chip (empty if none)

### ChipSystemConfig
Global chip system settings:
- `unlockedSlots` - Number of slots player has unlocked
- `maxSlots` - Maximum unlockable slots (default: 10)
- `baseSlotCost` - Base Prism cost for first slot
- `slotCostMultiplier` - Cost increase per slot
- `basePurchaseCost` - Prism cost to purchase a chip

## ChipService API

### Core Methods

**Initialization**
- `GetDefinition(string chipId)` - Get chip definition
- `GetAllDefinitions()` - Get all chip definitions
- `GetUnlockedDefinitions()` - Get only unlocked chips

**Progress & Collection**
- `GetProgress(string chipId)` - Get chip progress data
- `IsChipUnlocked(string chipId)` - Check if unlocked
- `TryAddChip(string chipId, int count = 1)` - Add chips (auto-upgrades rarity)

**Slot Management**
- `GetUnlockedSlotCount()` - Number of unlocked slots
- `GetMaxSlotCount()` - Maximum slots
- `GetNextSlotCost()` - Cost to unlock next slot
- `CanUnlockSlot()` - Check if player can afford
- `TryUnlockSlot()` - Unlock a new slot (costs Prisms)

**Equip Management**
- `GetEquippedChip(int slotIndex)` - Get chip in slot
- `IsChipEquipped(string chipId)` - Check if already equipped
- `CanEquipChip(string chipId, int slotIndex)` - Check if can equip
- `TryEquipChip(string chipId, int slotIndex)` - Equip chip to slot
- `TryUnequipChip(int slotIndex)` - Remove chip from slot

**Bonus Calculation**
- `GetActiveChipBonuses()` - Dictionary of all active bonuses
- `GetBonusValue(ChipBonusType type)` - Get total bonus for type

**Purchase System**
- `GetChipPurchaseCost()` - Cost to purchase random chip
- `CanPurchaseChip()` - Check if can afford
- `TryPurchaseRandomChip(out string chipId)` - Purchase random chip (excludes maxed chips)

### Events
ChipService fires these events:
- `ChipUnlocked` - When first chip of a type is collected
- `ChipUpgraded` - When rarity increases
- `ChipEquipped` - When chip equipped to slot
- `ChipUnequipped` - When chip removed from slot
- `SlotUnlocked` - When new slot is purchased

All events are also forwarded to EventManager.

## UI Integration

### ChipSelectorUI
- **Location**: `Code/Scripts/UI/MainMenu/ChipSelectorUI.cs`
- Displays all chips (locked chips show as "???")
- Shows chip slots with lock/empty/equipped states
- Purchase button for random chip acquisition
- Unlock slot button with cost display
- Detailed chip info panel with rarity colors
- Equip/unequip functionality

### LoadoutScreen Integration
Add a button to open ChipSelectorUI:
```csharp
[Header("Chip Selection")]
[SerializeField] private GameObject chipSelectionPanel;
[SerializeField] private Button openChipSelectorButton;
```

## Round Integration

### RoundManager
Chips are applied at round start via `ApplyChipBonuses()` method:
- Called during `ApplyRoundStartBonuses()`
- Retrieves active chip bonuses from ChipService
- Maps bonuses to skill modifiers or direct effects
- Example: `FragmentsBoost` increases starting fragments

### Example Bonus Application
```csharp
case ChipBonusType.FragmentsBoost:
    float currentFragments = roundWallet.Get(CurrencyType.Fragments);
    float boostedFragments = currentFragments * (1f + bonus.Value / 100f);
    float difference = boostedFragments - currentFragments;
    if (difference > 0)
        roundWallet.Add(CurrencyType.Fragments, difference);
    break;
```

## Creating New Chips

1. **Create ScriptableObject**
   - Right-click in Project > Create > Outline > ChipDefinition
   
2. **Configure Properties**
   - Set unique ID
   - Add name, description, icon
   - Choose bonus type
   - Set base bonus and growth
   - Configure rarity thresholds
   
3. **Add to ChipService**
   - Add definition to ChipService's `loadedDefinitions` array in Inspector
   
4. **Implement Bonus Logic** (if new bonus type)
   - Add to `ChipBonusType` enum
   - Add case to `ApplyChipBonuses()` in RoundManager
   - Or integrate with skill system

## Example Chip Configuration

**Attack Boost Chip**
```
ID: "ATK_BOOST_01"
Name: "Power Surge"
Description: "Increases attack damage by a percentage"
Bonus Type: AttackDamageMultiplier
Base Bonus: 5.0 (5%)
Bonus Per Rarity: 3.0 (3% per level)
Bonus Format: "+{0}%"
Chips Needed: [0, 3, 5, 7, 10]
Can Change In Round: true
```

At Legendary (rarity 4): 5 + (3 × 4) = 17% attack damage bonus

## Event Names
Chip events in `EventNames.cs`:
- `EventNames.ChipUnlocked`
- `EventNames.ChipUpgraded`
- `EventNames.ChipEquipped`
- `EventNames.ChipUnequipped`
- `EventNames.ChipSlotUnlocked`

## Economy

### Costs
- **Chip Purchase**: Prisms (configurable via `ChipSystemConfig.basePurchaseCost`)
- **Slot Unlock**: Prisms (increases with each slot via exponential formula)
  - Formula: `baseSlotCost × (slotCostMultiplier ^ (slotIndex - 1))`
  - Example: 100, 150, 225, 337, 506...

### Rewards
Chips can be obtained through:
- Purchase system (random selection)
- Potential future integrations:
  - Achievement rewards
  - Objective rewards
  - Wave completion bonuses
  - Daily login rewards

## Extensibility

### Adding New Bonus Types
1. Add to `ChipBonusType` enum in `ChipDefinition.cs`
2. Implement logic in `RoundManager.ApplyChipBonuses()`
3. Or integrate with skill system for automatic application

### Custom Rarity Progression
Modify `chipsNeededForRarity` array per chip for unique progression curves

### Advanced Features (Future)
- Chip sets (bonuses for equipping multiple related chips)
- Chip fusion (combine chips for special variants)
- Temporary chips (time-limited powerful chips)
- Chip challenges (special objectives for specific chips)

## Testing Checklist
- [ ] Create sample chip definitions
- [ ] Test chip purchase system
- [ ] Test slot unlocking
- [ ] Test chip equipping/unequipping
- [ ] Test rarity progression
- [ ] Test bonus application in rounds
- [ ] Test locked chip display ("???")
- [ ] Test maxed chip exclusion from purchase pool
- [ ] Test save/load persistence
- [ ] Test UI refresh on state changes

## Notes
- Chips can only be equipped to unlocked slots
- Each chip can only be equipped once (no duplicates)
- Maxed chips (Legendary) are excluded from purchase pool
- First chip unlock reveals the chip to the player
- Bonuses stack additively within the same type
