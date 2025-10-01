# Unity Setup Checklist for Loadout Tab

Use this checklist to ensure proper setup of the Loadout tab in Unity Editor.

## Prerequisites
- [ ] Code has been pulled from the PR branch
- [ ] Unity project opens without errors
- [ ] MainMenu scene is loaded

## Setup Steps

### 1. Create LoadoutScreen GameObject
- [ ] Create new GameObject named "LoadoutScreen" under Canvas
- [ ] Position at same level as MainScreen, UpgradeScreen, etc.
- [ ] Add LoadoutScreen component
- [ ] Initially disable the GameObject (unchecked in Inspector)

### 2. Configure LoadoutScreen Component

#### Tower Base Section
- [ ] Assign `Tower Base Image` (Image component for preview)
- [ ] Assign `Tower Base Description` (TextMeshProUGUI component)

#### Turret Selection Section
- [ ] Assign `Turret Selection Panel` (the popup panel GameObject)
- [ ] Add Turret Slot Buttons to list:
  - [ ] Slot Button 1
  - [ ] Slot Button 2
  - [ ] Slot Button 3
  - [ ] Slot Button 4

#### Turret Slots Settings
- [ ] `Skill Service` - Leave empty (auto-finds SkillService.Instance)
- [ ] `Turret Slots Skill Id` - Set to "TurretSlots" (default)
- [ ] `Default Total Slots` - Set to 1
- [ ] `Debug Override Total Slots` - Set to -1 (disabled)

### 3. Update MainMenuUIManager

- [ ] Select GameObject with MainMenuUIManager component
- [ ] In "Main Menu Footer Screens" section:
  - [ ] Assign LoadoutScreen GameObject to `Loadout Screen UI` field
- [ ] In "Main Menu Footer Buttons" section:
  - [ ] Create or find Loadout button
  - [ ] Assign to `Loadout Button` field

### 4. Create Loadout Navigation Button

- [ ] Duplicate an existing footer button (e.g., PLAY button)
- [ ] Rename to "LoadoutButton"
- [ ] Change button text to "LOADOUT"
- [ ] Position in footer navigation bar (suggested: between PLAY and UPGRADE)
- [ ] Configure OnClick event:
  - [ ] Drag MainMenuUIManager GameObject
  - [ ] Select function: `MainMenuUIManager.SelectLoadoutScreen()`

### 5. Move UI Elements to LoadoutScreen

#### From MainScreen to LoadoutScreen:
- [ ] Tower Base section (Image + Description + Change button)
- [ ] Turret Selection section (4 slot buttons + selection panel)
- [ ] Ensure parent-child relationships are maintained
- [ ] Verify all elements are children of LoadoutScreen GameObject

### 6. Update TurretSelectorUI Reference

- [ ] Find GameObject with TurretSelectorUI component
- [ ] Verify it's referenced by LoadoutScreen's `Turret Selection Panel` field
- [ ] No code changes needed (LoadoutScreen handles the setup)

### 7. Verify MainScreen Cleanup

MainScreen (PLAY tab) should now only contain:
- [ ] Player username display
- [ ] Username change panel
- [ ] Difficulty selector
- [ ] Play button
- [ ] No tower base elements
- [ ] No turret selection elements

## Testing Checklist

### Basic Navigation
- [ ] Enter Play Mode
- [ ] PLAY tab is shown by default
- [ ] Click LOADOUT tab - switches correctly
- [ ] Click back to PLAY tab - switches correctly
- [ ] All other tabs (UPGRADE, REWARD, etc.) still work

### Tower Base Selection
- [ ] In LOADOUT tab, tower base is displayed
- [ ] Click "Change Tower Base" button (or similar)
- [ ] Tower Base selector popup opens
- [ ] Select a different tower base
- [ ] Preview updates in LOADOUT tab
- [ ] Selection persists when switching tabs

### Turret Selection
- [ ] In LOADOUT tab, all unlocked slots show correctly
- [ ] Locked slots show "Locked" and are grayed out
- [ ] Click an unlocked slot
- [ ] Turret selector opens
- [ ] Select a turret
- [ ] Slot updates with selected turret
- [ ] Selection persists when switching tabs

### Slot Unlocking
- [ ] By default, only 1 slot is unlocked
- [ ] Locked slots cannot be clicked
- [ ] Unlock more slots via skill system (if implemented)
- [ ] Newly unlocked slots become clickable
- [ ] Slot count matches skill value

### Data Persistence
- [ ] Configure loadout (tower + turrets)
- [ ] Start a game
- [ ] Return to main menu
- [ ] Loadout configuration is saved
- [ ] All selections are remembered

### Error Handling
- [ ] No NullReferenceExceptions in console
- [ ] No missing reference warnings
- [ ] EventSystem exists (created automatically if needed)
- [ ] All buttons respond to clicks

## Troubleshooting

### Issue: LoadoutScreen doesn't show when clicking LOADOUT button
**Check:**
- [ ] LoadoutScreen GameObject is assigned to MainMenuUIManager
- [ ] Button OnClick event is properly configured
- [ ] LoadoutScreen is at correct hierarchy level

### Issue: Tower base preview doesn't update
**Check:**
- [ ] Tower Base Image is assigned in LoadoutScreen
- [ ] Tower Base Description is assigned
- [ ] TowerBaseSelectorUI is calling LoadoutScreen.SetTowerBaseImage()

### Issue: Turret slots show as "Empty" or don't update
**Check:**
- [ ] All slot buttons are added to slotButtons list in LoadoutScreen
- [ ] Slot buttons have correct child structure (Info/TurretName, etc.)
- [ ] TurretSelectorUI is properly configured
- [ ] PlayerManager has turret data

### Issue: "Locked" slots still clickable
**Check:**
- [ ] SkillService is properly initialized
- [ ] TurretSlots skill exists in your skill definitions
- [ ] Slot unlocking logic is working (test with debugOverrideTotalSlots)

### Issue: Compilation errors
**Check:**
- [ ] All files were properly pulled from PR
- [ ] Unity project was refreshed/recompiled
- [ ] No conflicting or missing dependencies

## Visual Polish (Optional)

After basic functionality works:
- [ ] Adjust layout and spacing
- [ ] Add transition animations between tabs
- [ ] Enhance button hover effects
- [ ] Add tooltips for locked slots
- [ ] Improve visual feedback for selections
- [ ] Add sound effects for interactions

## Final Verification

Before merging:
- [ ] All checklist items completed
- [ ] No console errors or warnings
- [ ] All functionality tested and working
- [ ] Loadout persists correctly
- [ ] User experience is improved
- [ ] Ready for production use

## Documentation Reference

For detailed information, see:
- `README_LOADOUT_TAB.md` - Quick overview
- `LOADOUT_TAB_IMPLEMENTATION.md` - Detailed setup guide
- `IMPLEMENTATION_STEPS.md` - Technical details
- `ARCHITECTURE_CHANGES.md` - Before/after comparison

## Time Estimates

- Basic setup: 30-45 minutes
- Testing: 15-30 minutes
- Visual polish: 30-60 minutes (optional)
- **Total: 45-135 minutes**

## Success Criteria

✅ **Setup Complete When:**
- All checklist items are checked
- No errors in console
- All functionality works as expected
- User experience is improved
- Ready to merge and deploy

---

**Last Updated**: 2024
**Unity Version**: Compatible with 2021.3+
**Status**: Ready for setup
