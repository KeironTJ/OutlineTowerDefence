# Research System - Unity Setup Checklist

This checklist guides you through setting up the Research System in the Unity Editor after the code implementation.

## Prerequisites
- ✓ All C# scripts compiled successfully
- ✓ No compilation errors in Unity Console

## Step 1: Create ResearchService GameObject

1. Open your main/persistent scene (where other singleton services exist)
2. Create new GameObject: `GameObject → Create Empty`
3. Rename to: `ResearchService`
4. Add Component: `Research Service` script
5. Verify the component is attached correctly

**Inspector Validation:**
- Component shows "Loaded Definitions" array (empty is OK for now)

## Step 2: Create Example Research Definitions

### Example 1: Attack Damage Research (BaseStat)

1. In Project window: `Right-click → Create → Game → Research Definition`
2. Rename to: `Research_AttackDamage`
3. Configure in Inspector:

```
Identity:
- ID: RESEARCH_ATK_DMG
- Display Name: Attack Damage Research
- Description: Increase tower attack damage permanently
- Icon: [Assign an appropriate icon sprite]
- Research Type: BaseStat

Progression:
- Max Level: 10

Time Configuration:
- Base Time Seconds: 60 (1 minute for testing)
- Time Growth Factor: 1.5
- Time Curve: Exponential

Cost Configuration - Cores:
- Base Core Cost: 100
- Core Cost Growth Factor: 1.5
- Core Cost Curve: Exponential

Speed-Up Configuration:
- Loops Per Hour Speedup: 10

Instant Complete Configuration:
- Prisms Per Hour Instant: 50

Prerequisites:
- (Leave empty for first research)

Effects:
- Unlock Target ID: (Leave empty for BaseStat)
- Stat Bonuses: [Add one element]
  - Target Stat: AttackDamage
  - Contribution Kind: FlatBonus
  - Value: 5.0
  - Pipeline Scale: 1
  - Pipeline Min: -Infinity
  - Pipeline Max: Infinity
```

### Example 2: Tower Base Unlock

1. Create another Research Definition: `Research_TowerBase_Heavy`
2. Configure:

```
Identity:
- ID: RESEARCH_TOWER_BASE_HEAVY
- Display Name: Heavy Tower Base
- Description: Unlock the heavy-duty tower base
- Icon: [Assign icon]
- Research Type: TowerBase

Progression:
- Max Level: 1

Time Configuration:
- Base Time Seconds: 30 (30 seconds for testing)
- Time Growth Factor: 1.0
- Time Curve: Linear

Cost Configuration - Cores:
- Base Core Cost: 500
- Core Cost Growth Factor: 1.0
- Core Cost Curve: Linear

Speed-Up Configuration:
- Loops Per Hour Speedup: 10

Instant Complete Configuration:
- Prisms Per Hour Instant: 50

Prerequisites:
- (Leave empty or add prerequisite research ID)

Effects:
- Unlock Target ID: 0004 (or your heavy tower base ID)
- Stat Bonuses: (Leave empty for unlock type)
```

### Example 3: Turret Unlock

Create `Research_Turret_Sniper`:

```
Identity:
- ID: RESEARCH_TURRET_SNIPER
- Display Name: Sniper Turret
- Description: Unlock the long-range sniper turret
- Research Type: Turret
- Max Level: 1

Time/Cost: (Similar to tower base)
Effects:
- Unlock Target ID: SNP (or your turret ID)
```

### Example 4: Projectile Unlock

Create `Research_Projectile_Explosive`:

```
Identity:
- ID: RESEARCH_PROJECTILE_EXPLOSIVE
- Display Name: Explosive Rounds
- Description: Unlock explosive ammunition
- Research Type: Projectile
- Max Level: 1

Time/Cost: (Similar to tower base)
Effects:
- Unlock Target ID: EXP_BULLET (or your projectile ID)
```

## Step 3: Assign Definitions to ResearchService

1. Select `ResearchService` GameObject in Hierarchy
2. In Inspector, expand `Loaded Definitions` array
3. Set Size: 4 (or number of definitions you created)
4. Drag each ResearchDefinition asset to the array slots
5. Verify all slots are filled

## Step 4: Create Research UI Prefab

### Create Research Card Prefab

1. In Hierarchy: `Right-click → UI → Panel`
2. Rename to: `ResearchCard`
3. Add components and child objects:

**ResearchCard (Panel)**
- Add Component: `Research Card View`
- Add Component: `Button` (for click handling)

**Child Objects to Create:**
```
ResearchCard/
├── Icon (Image)
├── ResearchNameText (TextMeshPro)
├── DescriptionText (TextMeshPro)
├── LevelText (TextMeshPro)
├── CurrentValueText (TextMeshPro)
├── NextValueText (TextMeshPro)
├── TimeRemainingText (TextMeshPro)
├── CostText (TextMeshPro)
├── LockedPanel (Panel - overlay when locked)
├── ResearchingPanel (Panel - overlay when researching)
│   └── ProgressBar (Image with Fill Type: Filled)
├── ActionButton (Button)
│   └── ButtonText (TextMeshPro)
├── SpeedUpButton (Button)
│   └── ButtonText (TextMeshPro)
└── InstantCompleteButton (Button)
    └── ButtonText (TextMeshPro)
```

4. Assign references in `Research Card View` component:
   - Action Button → ActionButton
   - Icon → Icon
   - Research Name Text → ResearchNameText
   - (Continue for all fields)

5. Save as Prefab: Drag to Project window under `Prefabs/UI/Research/`

## Step 5: Create Research Panel

1. Find `OptionsUIManager` in your scene hierarchy
2. Under OptionsUIManager, create new Panel:
   - `Right-click OptionsUIManager → UI → Panel`
   - Rename to: `ResearchPanel`

3. Add Component: `Research Panel UI`

4. Create child objects:
```
ResearchPanel/
├── Header (Panel)
│   ├── TitleText (TextMeshPro): "Research"
│   ├── ActiveResearchText (TextMeshPro)
│   ├── CoresBalanceText (TextMeshPro)
│   ├── LoopsBalanceText (TextMeshPro)
│   ├── PrismsBalanceText (TextMeshPro)
│   └── CloseButton (Button)
├── FilterButtons (Panel)
│   ├── FilterAllButton (Button)
│   ├── FilterTowerBaseButton (Button)
│   ├── FilterTurretButton (Button)
│   ├── FilterProjectileButton (Button)
│   └── FilterBaseStatButton (Button)
└── ScrollView
    └── Viewport
        └── Content (Vertical Layout Group)
            └── ResearchCardContainer (Empty, this is where cards spawn)
```

5. Configure `Research Panel UI` component:
   - Research Card Container → Content
   - Research Card Prefab → Your ResearchCard prefab
   - Close Button → CloseButton
   - Filter buttons → Assign each filter button
   - Info display texts → Assign each text field

6. Configure OptionsUIManager:
   - Research Panel → ResearchPanel GameObject
   - Research Panel UI → ResearchPanelUI component

## Step 6: Add Research Button to Options Menu

1. Find your Options menu panel
2. Add a new Button: `Research`
3. Position it with other option buttons (Profile, Stats, Chips, etc.)
4. Configure Button onClick:
   - Click `+` to add event
   - Drag OptionsUIManager to object field
   - Select Function: `OptionsUIManager.ShowResearch`

## Step 7: Give Player Starting Currency (For Testing)

1. Find `PlayerManager` in scene
2. Or edit your save file to add:
   ```json
   "cores": 10000,
   "loops": 1000,
   "prisms": 100
   ```

## Step 8: Test Basic Functionality

### Test 1: Open Research Panel
1. Play the game
2. Open Options menu
3. Click Research button
4. Verify panel opens with research cards

**Expected:**
- Research panel displays
- All research definitions show as cards
- Cards show locked/available state correctly
- Currency balances display

### Test 2: Start Research
1. Click "Start Research" on available research
2. Verify:
   - Cores deducted
   - Research state changes to "Researching"
   - Progress bar appears
   - Countdown timer starts

### Test 3: Progress Tracking
1. Wait and observe countdown
2. Verify:
   - Time decreases correctly
   - Progress bar fills
   - Completion triggers automatically

### Test 4: Speed Up
1. Start a research
2. Click "Speed Up" button
3. Verify:
   - Loops deducted
   - Time reduced
   - If time reaches zero, research completes

### Test 5: Instant Complete
1. Start a research
2. Click "Instant Complete"
3. Verify:
   - Prisms deducted (based on remaining time)
   - Research completes immediately
   - Effects applied (unlock or stat bonus)

### Test 6: Unlock Effects
1. Complete tower base research
2. Verify tower base is unlocked in game
3. Complete turret research
4. Verify turret is unlocked
5. Complete projectile research
6. Verify projectile is unlocked

### Test 7: Stat Bonuses
1. Complete BaseStat research
2. Check tower stats in game
3. Verify bonus applied correctly
4. Complete additional levels
5. Verify bonuses stack

### Test 8: Prerequisites
1. Create research with prerequisites
2. Verify it shows as locked until prereq complete
3. Complete prerequisite
4. Verify research becomes available

### Test 9: Offline Progress
1. Start a research
2. Note remaining time
3. Close and restart Unity/Game
4. Verify research completed if enough time passed
5. Verify notification shown

### Test 10: Filters
1. Click each filter button
2. Verify only matching research types display
3. Click "All" to show all again

## Step 9: Adjust for Production

Once testing is successful, adjust values for real gameplay:

1. **Increase Time Scales:**
   - Early research: 5-30 minutes
   - Mid research: 1-6 hours
   - Late research: 12-48 hours

2. **Increase Costs:**
   - Early: 1,000 - 10,000 cores
   - Mid: 100,000 - 1,000,000 cores
   - Late: 10,000,000+ cores

3. **Adjust Growth Factors:**
   - Time: 1.3 - 1.6 for good progression curve
   - Cost: 1.5 - 2.0 for meaningful choices

## Step 10: Polish

1. **Add Icons:**
   - Create/assign appropriate icons for each research
   - Consistent visual style

2. **Tune Audio:**
   - Button click sounds
   - Research start sound
   - Completion notification sound

3. **Animation:**
   - Button press animations
   - Progress bar smooth fill
   - Card appear/disappear transitions

4. **Visual Feedback:**
   - Highlight available research
   - Dim locked research
   - Pulse active research
   - Glow on completion

## Common Issues and Solutions

### Issue: ResearchService is null
**Solution:** Ensure ResearchService GameObject exists in scene and script is attached

### Issue: Definitions not showing in UI
**Solution:** Check definitions are assigned to ResearchService.loadedDefinitions array

### Issue: Can't start research (no currency)
**Solution:** Add currency through PlayerManager or save file editing

### Issue: Time not counting down
**Solution:** Verify Update() is being called (check service is enabled)

### Issue: UI not opening
**Solution:** Check OptionsUIManager has reference to ResearchPanel

### Issue: Buttons not working
**Solution:** Verify onClick events are set up correctly on buttons

### Issue: Progress not saving
**Solution:** Ensure PlayerManager.SavePlayerData() is being called

## Verification Checklist

After setup, verify:

- [ ] ResearchService exists in scene with definitions assigned
- [ ] At least 4 example research definitions created
- [ ] Research UI prefab created with all components
- [ ] Research Panel created under OptionsUIManager
- [ ] Research button added to Options menu
- [ ] Can open research panel from options
- [ ] Can see all research cards
- [ ] Can start research (with currency)
- [ ] Timer counts down correctly
- [ ] Progress bar fills correctly
- [ ] Research completes automatically
- [ ] Can speed up with loops
- [ ] Can instant complete with prisms
- [ ] Unlocks work (tower/turret/projectile)
- [ ] Stat bonuses apply correctly
- [ ] Prerequisites work correctly
- [ ] Filters work correctly
- [ ] Offline progress works
- [ ] Save/load preserves progress

## Next Steps

Once basic functionality is verified:

1. Create full set of research items for your game
2. Balance progression curves
3. Add visual polish (icons, animations, sounds)
4. Create research unlock rewards/achievements
5. Test with real players for feedback
6. Tune based on player data and feedback

## Support

Refer to documentation:
- RESEARCH_QUICK_START.md - User guide
- RESEARCH_IMPLEMENTATION_GUIDE.md - Technical details
- RESEARCH_FEATURE_SUMMARY.md - Overview

For issues, check:
- Unity Console for errors
- Event listeners are set up correctly
- All references are assigned in Inspector
- PlayerManager is initialized before use
