# Notification System Quick Start

## For Developers - Getting Started in 5 Minutes

### 1. Add NotificationManager to Your Scene

The NotificationManager should exist in your main scene or be instantiated early in your game:

```csharp
// In your game initialization script or bootstrap
GameObject notificationManager = new GameObject("NotificationManager");
notificationManager.AddComponent<NotificationManager>();
```

Or add it via the Unity Editor:
1. Create an empty GameObject named "NotificationManager"
2. Add the `NotificationManager` component
3. The manager will persist across scenes automatically

### 2. Create Notification UI

#### Quick Notification Popup (Auto-dismiss)

1. Create a UI panel for the quick notification
2. Add child elements:
   - TextMeshProUGUI for title
   - TextMeshProUGUI for description
   - Image for icon (optional)
3. Add the `NotificationPopupUI` component to the panel
4. Assign the UI elements in the inspector
5. Position it where you want notifications to appear (usually top-right or top-center)

#### Modal Notification (Requires interaction)

1. Create a full-screen modal panel
2. Add child elements:
   - TextMeshProUGUI for title
   - TextMeshProUGUI for description
   - TextMeshProUGUI for rewards list
   - Button for "Claim"
   - Button for "Close"
   - Image for icon (optional)
3. Add the `NotificationModalUI` component to the panel
4. Assign all UI elements in the inspector
5. Set the panel to inactive by default

### 3. Add Notification Indicators to Buttons

For any button that should show notification badges (Objectives, Achievements, Loadout, etc.):

**Option A: Use NotificationButtonHelper (Easiest)**
1. Select the button in Unity Editor
2. Add Component → `NotificationButtonHelper`
3. Set the `Notification Source` to the appropriate type:
   - `Achievement` for achievement buttons
   - `Objective` for objective buttons
   - `Loadout` for loadout buttons
   - etc.
4. Leave `Auto Create Badge` checked
5. Done! The badge will appear automatically when there are notifications

**Option B: Manual Setup**
1. Select the button in Unity Editor
2. Add Component → `NotificationIndicator`
3. Create a child GameObject named "NotificationBadge"
4. Add an Image component and set it to red
5. Add a TextMeshProUGUI child for the count
6. Assign the badge and text to the NotificationIndicator component
7. Set the source filter

### 4. Trigger Notifications from Code

#### Quick Notification (Simple)
```csharp
NotificationManager.Instance.ShowQuickNotification(
    "Achievement Unlocked!",
    "You completed your first mission!",
    NotificationSource.Achievement,
    duration: 3f
);
```

#### Modal Notification (With Rewards)
```csharp
var rewards = new NotificationReward[]
{
    new NotificationReward(
        NotificationRewardType.Currency, 
        "", 
        100, 
        CurrencyTypes.PrismShards
    )
};

NotificationManager.Instance.ShowModalNotification(
    "Daily Objective Complete!",
    "You've earned bonus rewards!",
    rewards,
    NotificationSource.Objective,
    "daily_obj_001"
);
```

### 5. Test It!

Run your game and:
1. Trigger an achievement or objective completion
2. Watch the notification appear
3. Check that the indicator badge shows on relevant buttons
4. Try claiming rewards from modal notifications

## Common Use Cases

### Notify on Achievement Tier Unlock
Already integrated! The NotificationManager automatically listens for achievement events.

### Notify on Objective Completion
Already integrated! Daily and Weekly objective managers trigger notifications.

### Notify on Chip Unlock
Already integrated! Listens to the ChipUnlocked event.

### Custom Notification
```csharp
void OnPlayerLevelUp()
{
    NotificationManager.Instance.ShowQuickNotification(
        "Level Up!",
        $"You reached level {playerLevel}!",
        NotificationSource.System,
        4f
    );
}
```

### Notification with Multiple Rewards
```csharp
void OnPackOpened(PackData pack)
{
    var rewards = new List<NotificationReward>();
    
    foreach (var item in pack.contents)
    {
        rewards.Add(new NotificationReward(
            NotificationRewardType.UnlockChip,
            item.chipId
        ));
    }
    
    NotificationManager.Instance.ShowModalNotification(
        "Pack Opened!",
        $"You opened {pack.name}!",
        rewards.ToArray(),
        NotificationSource.Store,
        pack.id
    );
}
```

## Troubleshooting

### Notifications Don't Appear
- Check that NotificationManager.Instance exists
- Ensure NotificationPopupUI or NotificationModalUI components are in your scene
- Check the Console for any error messages
- Verify the notification UI containers are active

### Indicators Don't Show
- Ensure the button has NotificationIndicator or NotificationButtonHelper
- Check that the source filter matches the notification source
- Verify the badge GameObject is assigned
- Listen for NotificationIndicatorUpdate events to debug

### Notifications Queue Up Too Much
- Adjust the `maxQueueSize` in NotificationManager
- Use Quick notifications for minor events
- Use Modal only for important events that need user attention
- Consider debouncing rapid events

## Next Steps

- Read the full [Notification System Guide](NOTIFICATION_SYSTEM_GUIDE.md)
- Customize notification appearance and animations
- Add sound effects for different notification types
- Create notification presets for common scenarios
- Consider adding a notification history panel

## Support

The notification system integrates with the existing EventManager system. All notifications are event-driven and modular, making it easy to extend for future features like:
- Timed research completions
- Store sales and promotions
- Social features (friend requests, clan invites)
- Limited-time events
