# Loadout Tab Implementation Guide

## Overview
This document describes the implementation of a dedicated "LOADOUT" tab in the Main Menu to house Tower Base and Turret Selection functionality, moving these elements from the "PLAY" tab for better organization.

## Changes Made

### 1. New Files Created

#### LoadoutScreen.cs
- **Location**: `Code/Scripts/UI/MainMenu/LoadoutScreen.cs`
- **Purpose**: Manages all loadout-related UI functionality
- **Features**:
  - Tower Base selection and display
  - Turret slot management with unlock gating
  - Skill-based slot unlocking (integrates with SkillService)
  - Modular design for future expansion (e.g., projectile selection)

### 2. Modified Files

#### MainMenuUIManager.cs
**Changes**:
- Added `Loadout` to `ScreenType` enum
- Added `loadoutScreenUI` GameObject reference (serialized field)
- Added `loadoutButton` GameObject reference (serialized field)
- Added `SelectLoadoutScreen()` method for navigation
- Updated `SelectScreen()` to handle Loadout tab activation
- Updated `ResetScreenButtonColors()` to include Loadout button

**Purpose**: Integrates the Loadout screen into the existing tab navigation system.

#### MainMenuScreen.cs
**Changes Removed**:
- Tower Base fields: `towerBaseImage`, `towerBaseDescription`
- Turret Selection fields: `turretSelectionPanel`, `slotButtons`
- SkillService fields for turret slot gating
- Method: `SetTowerBaseImage()`
- Methods: `UpdateSlotButtons()`, `OpenTurretSelection()`, `CloseTurretSelection()`, `OpenTurretVisualSelection()`
- Method: `GetUnlockedSlotsCount()`

**Remaining Functionality**:
- Player username display and editing
- Difficulty selection
- Game start functionality

**Purpose**: Focuses the PLAY tab on starting the game rather than loadout configuration.

#### TurretSelectorUI.cs
**Changes**:
- Removed `MainMenuScreen mainMenu` field
- Added `LoadoutScreen loadoutScreen` field
- Removed `SetMainMenu()` method
- Added `SetLoadoutScreen()` method
- Updated `OnPicked()` to refresh LoadoutScreen instead of MainMenuScreen
- Updated unlock callback to refresh LoadoutScreen

**Purpose**: Redirects turret selection callbacks to the new LoadoutScreen.

#### TowerBaseSelectorUI.cs
**Changes**:
- Updated tower selection callback to refresh LoadoutScreen instead of MainMenuScreen

**Purpose**: Redirects tower base selection callbacks to the new LoadoutScreen.

## Unity Editor Setup Instructions

### Step 1: Prepare the Loadout Screen GameObject

1. Open the MainMenu scene in Unity Editor
2. Locate the existing UI elements for Tower Base and Turret Selection (currently in the Main/PLAY screen)
3. Create a new GameObject under the Canvas hierarchy
   - Name it: `LoadoutScreen`
   - Position it at the same level as `MainScreen`, `UpgradeScreen`, etc.

### Step 2: Configure the LoadoutScreen Component

1. Select the `LoadoutScreen` GameObject
2. In the Inspector, click "Add Component"
3. Add the `LoadoutScreen` script
4. Configure the serialized fields:

   **Tower Base:**
   - `Tower Base Image`: Drag the Image component that displays the tower base preview
   - `Tower Base Description`: Drag the TextMeshProUGUI component that shows the description

   **Turret Selection:**
   - `Turret Selection Panel`: Drag the panel GameObject that contains the turret selector UI
   - `Slot Buttons`: Add the Button components for each turret slot (up to 4 slots)

   **Turret Slots (simple gating):**
   - `Skill Service`: Will auto-find SkillService.Instance, or manually assign if needed
   - `Turret Slots Skill Id`: Keep default "TurretSlots" (or modify if your skill ID differs)
   - `Default Total Slots`: Set to 1 (default fallback)
   - `Debug Override Total Slots`: Set to -1 (disabled) or a positive number for testing

### Step 3: Update MainMenuUIManager

1. Select the GameObject that has the `MainMenuUIManager` component
2. In the Inspector, locate the serialized fields:

   **Main Menu Footer Screens:**
   - Find the new `Loadout Screen UI` field
   - Drag the `LoadoutScreen` GameObject into this field

   **Main Menu Footer Buttons:**
   - Find the new `Loadout Button` field
   - Drag the button GameObject that will navigate to the Loadout screen

### Step 4: Create and Wire Up the Loadout Navigation Button

1. Locate the footer navigation buttons (PLAY, UPGRADE, REWARD, RESEARCH, SETTINGS)
2. Duplicate one of the existing buttons (e.g., the PLAY button)
3. Rename it to "LoadoutButton" or similar
4. Update the button text to "LOADOUT"
5. Position it appropriately in the navigation footer
6. Select the button and in the Inspector:
   - Find the `OnClick()` event section
   - Add a new event
   - Drag the `MainMenuUIManager` GameObject into the Object field
   - Select Function: `MainMenuUIManager.SelectLoadoutScreen()`

### Step 5: Move UI Elements to LoadoutScreen

1. Identify the UI elements for Tower Base and Turret Selection currently in MainScreen
2. Move these GameObjects to be children of the `LoadoutScreen` GameObject
3. Ensure the hierarchy is preserved (parent-child relationships)
4. Update any references if needed

### Step 6: Update TurretSelectorUI References

1. Find the GameObject with the `TurretSelectorUI` component
2. The component now needs to be called with `SetLoadoutScreen(LoadoutScreen)` instead of `SetMainMenu(MainMenuScreen)`
3. The `LoadoutScreen.OpenTurretVisualSelection()` method handles this automatically

### Step 7: Test the Implementation

1. Enter Play Mode in Unity
2. Verify the navigation:
   - Click the PLAY tab → Should show username, difficulty, play button (no loadout elements)
   - Click the LOADOUT tab → Should show tower base selector and turret slots
3. Test Tower Base selection:
   - Click on tower base selector
   - Select a different tower base
   - Verify the preview updates
4. Test Turret selection:
   - Click on a turret slot
   - Select a turret
   - Verify the slot updates with the selected turret
5. Test slot locking:
   - Verify that locked slots show "Locked" and are not clickable
   - Verify that unlocking slots via the skill system enables additional slots

## Architecture Benefits

### Separation of Concerns
- **PLAY Tab (MainMenuScreen)**: Focused on starting the game
  - Player identification
  - Difficulty selection
  - Game start button

- **LOADOUT Tab (LoadoutScreen)**: Focused on customization
  - Tower base selection
  - Turret loadout configuration
  - Future: Projectile selection, loadout presets

### Modularity
- Easy to extend LoadoutScreen with new features
- Clear boundaries between game start and loadout configuration
- Follows Single Responsibility Principle

### Maintainability
- Smaller, more focused classes
- Easier to locate and modify loadout-related code
- Reduces cognitive load when working on specific features

## Future Enhancements

The LoadoutScreen is designed to accommodate future features:

1. **Projectile Selection**
   - Add a new section similar to turret selection
   - Implement projectile slot buttons
   - Add projectile selector UI panel

2. **Loadout Presets**
   - Save/load multiple loadout configurations
   - Quick-switch between favorite setups
   - Name and organize presets

3. **Visual Improvements**
   - 3D tower base preview
   - Animated turret previews
   - Detailed stat comparisons

4. **Synergy System**
   - Show synergies between selected towers and turrets
   - Highlight optimal combinations
   - Display loadout power ratings

## Troubleshooting

### Issue: NullReferenceException when opening LoadoutScreen
**Solution**: Verify all serialized fields in LoadoutScreen are properly assigned in the Inspector.

### Issue: Buttons not responding
**Solution**: Ensure EventSystem exists in the scene. LoadoutScreen creates one automatically if missing.

### Issue: Tower/Turret selection not updating
**Solution**: Check that TowerBaseSelectorUI and TurretSelectorUI are calling the correct methods on LoadoutScreen.

### Issue: Slots not unlocking
**Solution**: Verify SkillService is properly initialized and the "TurretSlots" skill exists in your skill definitions.

## Code Reference

### Key Methods in LoadoutScreen

- `SetTowerBaseImage()`: Updates the tower base preview based on player's selection
- `UpdateSlotButtons()`: Refreshes all turret slot button states
- `GetUnlockedSlotsCount()`: Determines how many turret slots are unlocked via skills
- `OpenTurretVisualSelection(int slotIndex)`: Opens the turret selector for a specific slot
- `OpenTurretSelection()`: Legacy method for opening turret selection panel
- `CloseTurretSelection()`: Closes the turret selection panel

### Key Methods in MainMenuUIManager

- `SelectLoadoutScreen()`: Navigates to the Loadout tab
- `SelectScreen(ScreenType screenType)`: Core navigation method (updated to include Loadout)

## Files Affected Summary

```
Code/Scripts/UI/MainMenu/
├── LoadoutScreen.cs (NEW)
├── LoadoutScreen.cs.meta (NEW)
├── MainMenuScreen.cs (MODIFIED - reduced responsibility)
└── MainMenuUIManager.cs (MODIFIED - added Loadout support)

Code/Scripts/Gameplay/Tower/
└── TowerBaseSelectorUI.cs (MODIFIED - updated callback)

Code/Scripts/Gameplay/Turret/
└── TurretSelectorUI.cs (MODIFIED - updated to use LoadoutScreen)
```

## Testing Checklist

- [ ] Main Menu loads without errors
- [ ] PLAY tab displays correctly (username, difficulty, play button)
- [ ] LOADOUT tab displays correctly (tower base, turret slots)
- [ ] Navigation between tabs works smoothly
- [ ] Tower base selection works and updates preview
- [ ] Turret selection works for unlocked slots
- [ ] Locked slots show correct UI and cannot be opened
- [ ] Skill-based slot unlocking works correctly
- [ ] Multiple turret slots can be configured independently
- [ ] Changes persist when switching between tabs
- [ ] Changes are saved when starting a game

---

**Implementation Date**: 2024
**Unity Version**: Compatible with Unity 2021.3+ (adjust as needed)
**Status**: Code Complete - Unity Editor Setup Required
