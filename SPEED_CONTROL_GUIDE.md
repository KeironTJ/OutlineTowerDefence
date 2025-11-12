# Round Speedup and Pause System

## Overview
The Round Speedup and Pause system allows players to control the speed of in-round gameplay, from pausing (0x) to accelerating up to 5x normal speed.

## Key Features

### Speed Control
- **Base Speed**: Players start with 1x speed (normal) and the ability to pause (0x)
- **Speed Increments**: Speeds increase in 0.25x increments (1.25x, 1.5x, 1.75x, etc.)
- **Maximum Speed**: Up to 5x speed, unlocked progressively via research
- **Pause**: Players can pause the game at any time (0x speed)

### Research Integration
Speed levels are unlocked through the research system:
- `RES_SPEED_125`: Unlocks 1.25x speed
- `RES_SPEED_150`: Unlocks 1.5x speed (requires RES_SPEED_125)
- `RES_SPEED_200`: Unlocks 2x speed (requires RES_SPEED_150)
- Additional research items can be created following the same pattern up to 5x

### Auto-Pause on Options Menu
- When the options menu (or any child panel) is opened, the game can automatically pause
- This behavior can be enabled/disabled in the Settings panel
- When the menu is closed, the game returns to its previous speed
- Default: Auto-pause is enabled

### What Gets Affected
The time scale affects:
- Enemy movement and attacks
- Tower attacks and cooldowns
- Projectile movement
- Healing rates
- Wave timers
- All in-round gameplay mechanics

### What Continues Normally
These systems use real-time and are NOT affected by speed/pause:
- Research progress (uses DateTime.UtcNow)
- UI updates
- Persistent systems

## Components

### TimeScaleManager
**Location**: `Code/Scripts/Services/TimeScaleManager.cs`

Singleton service that manages the game's time scale.

**Key Methods**:
- `SetSpeed(float speed)`: Set specific speed (clamped to unlocked range)
- `IncreaseSpeed()`: Increase by 0.25x
- `DecreaseSpeed()`: Decrease by 0.25x
- `Pause()`: Pause the game
- `Resume()`: Resume from pause (goes to 1x)
- `TogglePause()`: Toggle pause state
- `SetMaxUnlockedSpeed(float)`: Set max speed available (called by research system)

**Events**:
- `SpeedChanged(float)`: Fired when speed changes
- `Paused()`: Fired when game pauses
- `Resumed()`: Fired when game resumes

**Properties**:
- `CurrentSpeed`: Current time scale
- `MaxUnlockedSpeed`: Maximum speed unlocked via research
- `IsPaused`: Whether game is paused
- `AutoPauseOnOptions`: Enable/disable auto-pause on options menu

### SpeedUnlockManager
**Location**: `Code/Scripts/Services/SpeedUnlockManager.cs`

Manages unlocking speed levels through research completion.

**Configuration**:
- `speedUnlockResearchIds`: Array of research IDs that unlock each speed tier

**Behavior**:
- Listens for `ResearchCompleted` events
- Automatically updates `TimeScaleManager` when speed research is completed
- Maintains synchronization between research progress and available speeds

### SpeedControlUI
**Location**: `Code/Scripts/UI/Round/SpeedControlUI.cs`

UI component for displaying and controlling game speed.

**UI Elements**:
- Pause/Resume button
- Decrease speed button (left arrow)
- Increase speed button (right arrow)
- Speed display text (shows current speed or "PAUSED")

**Behavior**:
- Buttons are enabled/disabled based on current speed and unlocked speeds
- Updates automatically when speed changes
- Can use icons or text for buttons

### SettingsPanel
**Location**: `Code/Scripts/UI/GlobalUI/SettingsUI/SettingsPanel.cs`

Settings panel with toggle for auto-pause behavior.

**UI Elements**:
- `autoPauseToggle`: Toggle control for auto-pause setting
- `autoPauseLabel`: Label describing the setting

## Events

The system uses the following events (defined in `EventNames.cs`):

- `TimeScaleChanged`: Fired when time scale changes (includes speed value)
- `GamePaused`: Fired when game pauses
- `GameResumed`: Fired when game resumes
- `OptionsMenuOpened`: Fired when options menu opens
- `OptionsMenuClosed`: Fired when options menu closes

## Setup Instructions

### 1. Add TimeScaleManager to Scene
Create an empty GameObject in your game scene and add the `TimeScaleManager` component.
This will automatically initialize as a singleton.

### 2. Add SpeedUnlockManager to Scene
Add the `SpeedUnlockManager` component to the same GameObject or another persistent object.
This will automatically sync with research progress.

### 3. Add SpeedControlUI to HUD
Add `SpeedControlUI` component to your HUD Canvas and configure:
- Assign UI button references
- (Optional) Assign pause/play icons
- Position and style as desired

### 4. Configure SettingsPanel
In your Settings panel UI:
- Add a Toggle UI element for auto-pause
- Assign it to `autoPauseToggle` field in SettingsPanel
- Add a TextMeshProUGUI label and assign to `autoPauseLabel`

### 5. Create Research Assets
Research assets are already created for the first few tiers in `Resources/Data/Research/`:
- RES_SPEED_125.asset
- RES_SPEED_150.asset
- RES_SPEED_200.asset

To add more speed tiers:
1. Duplicate an existing speed research asset
2. Update the ID, name, description
3. Increase the cost and time requirements
4. Set prerequisites to the previous tier
5. Add the new research ID to `SpeedUnlockManager.speedUnlockResearchIds` array

### 6. Load Research Definitions
Make sure the ResearchService loads these definitions:
- Add the new research assets to `ResearchService.loadedDefinitions` in the Inspector

## Technical Details

### Time Scale Implementation
The system uses Unity's `Time.timeScale` property:
- `Time.timeScale = 0` for pause
- `Time.timeScale = 1` for normal speed
- `Time.timeScale = 2` for 2x speed, etc.

### Research Time Tracking
Research uses `DateTime.UtcNow` for time calculations, which is independent of `Time.timeScale`.
This ensures research continues even when the game is paused.

### Auto-Pause Implementation
- Listens for `OptionsMenuOpened` event
- Stores current speed before pausing
- Restores previous speed on `OptionsMenuClosed` event
- Setting is saved to PlayerPrefs

## Testing Checklist

- [ ] Pause works and completely stops gameplay
- [ ] Resume returns to previous speed
- [ ] Speed buttons correctly increase/decrease speed
- [ ] Speed buttons are disabled when at min/max
- [ ] Only unlocked speeds are accessible
- [ ] Research continues during pause
- [ ] Options menu auto-pause works (if enabled)
- [ ] Auto-pause setting persists across sessions
- [ ] Enemies, towers, cooldowns all respect time scale
- [ ] UI updates regardless of time scale

## Future Enhancements

- Add keyboard shortcuts for pause/speed control
- Add visual feedback for current speed (particles, animation speed)
- Add speed presets (quick jump to 1x, 2x, 3x)
- Add tooltips showing unlock requirements for locked speeds
- Add sound effects for speed changes
