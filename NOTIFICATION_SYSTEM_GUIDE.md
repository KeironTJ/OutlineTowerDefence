# Notification System Guide

## Overview
The notification system provides a comprehensive way to alert players about game events, achievements, rewards, and unlocks. It consists of two main notification types:
1. **Quick Notifications** - Auto-dismissing popups with a brief message
2. **Modal Notifications** - Full-screen notifications requiring user interaction (claim/close)

## Core Components

### NotificationManager
The central manager that handles notification queuing and display.
- **Location**: `Code/Scripts/Services/NotificationManager.cs`
- **Access**: `NotificationManager.Instance`

#### Key Methods:
```csharp
// Queue any notification
NotificationManager.Instance.QueueNotification(NotificationData notification);

// Show a quick notification (auto-dismiss)
NotificationManager.Instance.ShowQuickNotification(
    "Title", 
    "Description", 
    NotificationSource.Achievement, 
    duration: 3f
);

// Show a modal notification with rewards
NotificationManager.Instance.ShowModalNotification(
    "Title", 
    "Description", 
    rewards, 
    NotificationSource.Chip, 
    sourceId: "chip_id"
);

// Get pending notification count for a source
int count = NotificationManager.Instance.GetPendingCount(NotificationSource.Achievement);

// Dismiss current notification
NotificationManager.Instance.DismissCurrentNotification();
```

### NotificationData
Defines the structure of a notification.
- **Location**: `Code/Scripts/Events/Payloads/NotificationData.cs`

#### Properties:
- `NotificationType type` - Quick or Modal
- `string title` - Main heading
- `string description` - Detailed message
- `NotificationPriority priority` - Low, Normal, High, Critical
- `float displayDuration` - For quick notifications (seconds)
- `NotificationReward[] rewards` - Optional rewards to claim
- `NotificationSource source` - What triggered this notification
- `string sourceId` - ID of the source item

### NotificationSource Enum
Defines different sources of notifications:
- `System` - General system messages
- `Achievement` - Achievement completions
- `Objective` - Daily/Weekly objectives
- `Skill` - Skill unlocks
- `Chip` - Chip unlocks
- `Store` - Store purchases
- `Research` - Research completions (future)
- `Loadout` - Loadout changes

## UI Components

### NotificationPopupUI
Displays quick auto-dismissing notifications.
- **Location**: `Code/Scripts/UI/GlobalUI/NotificationPopupUI.cs`
- Automatically listens for `NotificationTriggered` events
- Supports both animation and fallback slide-in effects
- Can be dismissed early by user interaction

**Setup**:
1. Create a UI GameObject for the popup
2. Add `NotificationPopupUI` component
3. Assign UI element references (titleText, descriptionText, etc.)
4. Optionally add an Animator for smooth transitions

### NotificationModalUI
Displays modal notifications requiring user interaction.
- **Location**: `Code/Scripts/UI/GlobalUI/NotificationModalUI.cs`
- Automatically listens for `NotificationTriggered` events
- Handles reward claiming and granting
- Blocks user interaction until dismissed

**Setup**:
1. Create a modal panel GameObject
2. Add `NotificationModalUI` component
3. Assign UI element references (titleText, descriptionText, claimButton, etc.)
4. Modal will auto-show when modal notifications are triggered

### NotificationIndicator
Visual badge component for showing notification counts on buttons.
- **Location**: `Code/Scripts/UI/GlobalUI/NotificationIndicator.cs`
- Displays a pulsing red dot with optional count
- Filters notifications by source

**Setup**:
1. Add `NotificationIndicator` component to any UI button
2. Set the `sourceFilter` to the appropriate NotificationSource
3. Assign the `indicatorObject` (badge/dot GameObject)
4. Optionally assign `countText` for displaying numbers
5. Configure visual settings (color, pulse, etc.)

**Example Usage**:
```csharp
// On an Objectives button
var indicator = objectivesButton.GetComponent<NotificationIndicator>();
indicator.SetSourceFilter(NotificationSource.Objective);

// Get current count
int pendingObjectives = indicator.GetCount();
```

## Event Integration

The notification system integrates with Unity's EventManager:

### Event Names (added to EventNames.cs):
- `NotificationTriggered` - Fired when a notification is queued
- `NotificationDismissed` - Fired when a notification is dismissed
- `NotificationIndicatorUpdate` - Fired to refresh all indicators

### Auto-triggered Notifications:
The NotificationManager automatically creates notifications for:
- Achievement tier completions
- Chip unlocks
- Skill unlocks

### Manual Notification Triggers:
Objectives and other systems manually trigger notifications:
- Daily objective completion/claim
- Weekly objective completion/claim
- Custom game events

## Usage Examples

### Example 1: Simple Quick Notification
```csharp
NotificationManager.Instance.ShowQuickNotification(
    "Wave Complete!", 
    "You survived wave 10!", 
    NotificationSource.System
);
```

### Example 2: Reward Notification
```csharp
var rewards = new NotificationReward[]
{
    new NotificationReward(NotificationRewardType.Currency, "", 100, CurrencyTypes.PrismShards),
    new NotificationReward(NotificationRewardType.UnlockChip, "speed_chip_1")
};

NotificationManager.Instance.ShowModalNotification(
    "Achievement Unlocked!",
    "You've earned the Speed Demon achievement!",
    rewards,
    NotificationSource.Achievement,
    "speed_demon_achievement"
);
```

### Example 3: Adding Indicator to Button
```csharp
// In your UI script
[SerializeField] private Button achievementsButton;

void Start()
{
    // Add indicator component at runtime
    var indicator = achievementsButton.gameObject.AddComponent<NotificationIndicator>();
    indicator.SetSourceFilter(NotificationSource.Achievement);
    
    // Or configure existing indicator
    var existingIndicator = achievementsButton.GetComponent<NotificationIndicator>();
    if (existingIndicator != null)
    {
        existingIndicator.SetSourceFilter(NotificationSource.Achievement);
    }
}
```

## Integration Points

### Existing System Integrations:
1. **Achievements** - `AchievementManager.cs`
   - Triggers notification on tier claim
   
2. **Daily Objectives** - `DailyObjectiveManager.cs`
   - Triggers notification on completion (if manual claim)
   - Triggers notification on claim
   
3. **Weekly Objectives** - `WeeklyObjectiveManager.cs`
   - Triggers notification on completion (if manual claim)
   - Triggers notification on claim
   
4. **Chips** - Auto-handled by `NotificationManager`
   - Listens to `ChipUnlocked` event
   
5. **Skills** - Auto-handled by `NotificationManager`
   - Listens to `SkillUnlocked` event

### Adding Notifications to New Systems:
```csharp
// In your game system
void OnSomeImportantEvent()
{
    if (NotificationManager.Instance != null)
    {
        NotificationManager.Instance.ShowQuickNotification(
            "Important Event!",
            "Something cool happened!",
            NotificationSource.System,
            3f
        );
    }
}
```

## Best Practices

1. **Use Quick Notifications for**:
   - Progress updates
   - Minor achievements
   - System messages
   - Status changes

2. **Use Modal Notifications for**:
   - Major unlocks
   - Reward claiming
   - Important announcements
   - Critical information

3. **Notification Indicators**:
   - Always attach to relevant UI buttons
   - Use appropriate source filters
   - Keep the visual design consistent
   - Consider accessibility (color contrast, size)

4. **Performance**:
   - The system uses a queue to prevent notification spam
   - Maximum queue size is configurable (default: 50)
   - Quick notifications auto-dismiss to prevent buildup
   - Modal notifications pause the queue until dismissed

## Future Enhancements

Possible extensions to the notification system:
- Sound effects for different notification types
- Notification history panel
- Priority-based display ordering
- Rich text formatting for descriptions
- Icon mapping for different sources
- Notification persistence across sessions
- Push notification support (mobile)
- Notification categories/filtering
