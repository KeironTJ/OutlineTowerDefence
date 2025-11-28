# Chips System - Quick Start

## For Developers

### Setup (First Time)
1. Open Unity project
2. Go to menu: `Outline > Create Example Chips`
   - This creates 10 example chip definitions in `Assets/Resources/Data/Chips/`
3. Find ChipService GameObject in your main menu scene (or create one)
4. Add all chip definitions to ChipService's `loadedDefinitions` array
5. Add a "Chips" button to your LoadoutScreen UI
6. Connect the button to open the ChipSelectorUI panel

### Quick Integration Checklist
- [x] ChipDefinition.cs created
- [x] ChipService.cs created and added to scene
- [x] ChipSelectorUI.cs created
- [x] LoadoutScreen updated with chip button
- [x] RoundManager updated to apply chip bonuses
- [x] PlayerData updated with chip storage
- [x] Event system integration complete

### Creating Custom Chips
```
1. Right-click in Project > Create > Outline > ChipDefinition
2. Fill in the properties:
   - ID: Unique identifier (e.g., "MY_CHIP_01")
   - Name: Display name
   - Description: What it does
   - Bonus Type: Choose from dropdown
   - Base Bonus: Starting value (e.g., 5.0 for 5%)
   - Bonus Per Rarity: Growth per level (e.g., 3.0)
   - Chips Needed For Rarity: [0, 3, 5, 7, 10]
3. Add to ChipService's loadedDefinitions array
```

### Testing in Play Mode
```csharp
// In Unity Console or debug script:

// Give player some Prisms
PlayerManager.main.AddCurrency(prisms: 1000);

// Purchase a random chip
ChipService.Instance.TryPurchaseRandomChip(out string chipId);

// Unlock a chip slot
ChipService.Instance.TryUnlockSlot();

// Add specific chip directly (for testing)
ChipService.Instance.TryAddChip("ATK_BOOST_01", 5);

// Equip a chip
ChipService.Instance.TryEquipChip("ATK_BOOST_01", 0);

// Check active bonuses
var bonuses = ChipService.Instance.GetActiveChipBonuses();
foreach (var bonus in bonuses)
    Debug.Log($"{bonus.Key}: {bonus.Value}");
```

## For Players

### What are Chips?
Chips are special cards that give your tower permanent bonuses. Collect multiple copies of the same chip to increase its rarity and power!

### How to Get Chips
- Purchase chip packs using **Prisms** (secondary currency)
- Each purchase gives you a random chip
- Duplicate chips increase rarity instead of giving duplicates

### Rarity Levels
- **Common** (White) - Base level
- **Uncommon** (Green) - 3 chips collected
- **Rare** (Blue) - 5 chips collected
- **Epic** (Purple) - 7 chips collected  
- **Legendary** (Orange) - 10 chips collected (MAX)

### How to Use Chips
1. Purchase Prisms (earn from objectives, achievements, etc.)
2. Unlock chip slots (costs Prisms, gets more expensive)
3. Purchase chip packs to collect chips
4. Equip chips to active slots
5. Bonuses apply automatically when you start a round!

### Tips
- Unlock more slots to equip more chips
- Focus on upgrading your favorite chips to Legendary
- Different chips work better with different playstyles
- Mix and match chips for optimal builds
- Maxed chips (Legendary) won't appear in purchases anymore

## Chip Types

### Offensive
- **Power Surge**: Attack damage boost
- **Rapid Fire**: Attack speed boost
- **Lucky Shot**: Critical hit chance
- **Devastation**: Critical hit damage

### Defensive
- **Fortify**: Maximum health boost
- **Regeneration**: Health recovery speed

### Economic
- **Wealth**: Fragment earning boost
- **Knowledge**: Experience gain boost

### Utility
- **Velocity**: Projectile speed boost
- **Eagle Eye**: Turret range boost

## Economy

### Costs
- **Chip Purchase**: 50 Prisms (default)
- **First Slot**: 100 Prisms
- **Second Slot**: 150 Prisms
- **Third Slot**: 225 Prisms
- *(Increases exponentially)*

### Where to Get Prisms
- Daily objectives
- Weekly objectives
- Achievement rewards
- Special events
- (Future: In-app purchases)

## UI Guide

### Chip Selector Screen
- **Left Panel**: All available chips
  - Locked chips show as "???"
  - Click to see details
- **Top Section**: Your chip slots
  - Shows equipped chips
  - Click to select for equipping
- **Right Panel**: Chip details
  - Shows stats and rarity
  - Equip/unequip buttons
- **Bottom Buttons**:
  - **Purchase Chip**: Buy random chip pack
  - **Unlock Slot**: Purchase new equipment slot

### Loadout Screen
- Look for "Chips" or "Manage Chips" button
- Opens the Chip Selector

## Common Questions

**Q: Can I equip the same chip multiple times?**
A: No, each chip can only be equipped once.

**Q: What happens when I reach Legendary rarity?**
A: The chip reaches maximum power and won't appear in future purchases.

**Q: Do chip bonuses stack?**
A: Yes! Multiple different chips with similar bonuses will stack additively.

**Q: Can I change chips during a round?**
A: Most chips yes, but some special chips may lock in when equipped.

**Q: Do I lose chips if I die?**
A: No! Chips are permanent progression and saved to your account.

**Q: What if I don't have enough slots?**
A: Unlock more slots using Prisms. You start with 0 slots.

## Troubleshooting

### "No chips available for purchase"
- This means all your chips are maxed to Legendary
- Congratulations! You've completed the chip collection

### "Cannot equip chip"
- Make sure you have unlocked slots
- Check the chip isn't already equipped
- Select a valid slot first

### "Bonuses not applying"
- Bonuses apply at round start
- Exit to menu and start a new round
- Check your equipped chips in Loadout

## See Also
- Full documentation: `CHIPS_SYSTEM_GUIDE.md`
- Create custom chips: Right-click > Create > Outline > ChipDefinition
- Example chips: Use menu `Outline > Create Example Chips`
