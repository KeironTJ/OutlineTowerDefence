# Research System Implementation Guide

## Architecture Overview

The Research System follows the established patterns from ChipService and SkillService, providing a modular, event-driven architecture for time-based progression.

## Core Components

### 1. ResearchDefinition (ScriptableObject)
Location: `Code/Scripts/Data/Definitions/ResearchDefinition.cs`

Defines a researchable item with:
- Identity (ID, name, description, icon)
- Research type (TowerBase, Turret, Projectile, BaseStat)
- Time and cost progression curves
- Speed-up and instant completion costs
- Prerequisites
- Effects (unlocks or stat bonuses)

**Key Methods:**
- `GetTimeForLevel(int level)`: Calculates time for specific level
- `GetCoreCostForLevel(int level)`: Calculates cores cost for level
- `GetLoopsCostForSpeedup(float seconds)`: Calculates loops cost for speedup
- `GetPrismsCostForInstant(float seconds)`: Calculates prisms cost for instant completion

### 2. ResearchProgressData
Location: `Code/Scripts/Data/ResearchProgressData.cs`

Serializable data structure tracking individual research progress:
```csharp
public class ResearchProgressData
{
    public string researchId;
    public int currentLevel;
    public bool isResearching;
    public string startTimeIsoUtc;  // ISO 8601 timestamp
    public float durationSeconds;
}
```

### 3. ResearchService (Singleton)
Location: `Code/Scripts/Services/ResearchService.cs`

Main service managing all research operations.

**Key Responsibilities:**
- Definition indexing and access
- Progress tracking and state management
- Time calculation and validation
- Currency transactions
- Event dispatching
- Stat contribution (IStatContributor)

**Public API:**

```csharp
// Definition Access
ResearchDefinition GetDefinition(string researchId)
IEnumerable<ResearchDefinition> GetAllDefinitions()
IEnumerable<ResearchDefinition> GetByType(ResearchType type)

// Progress Access
ResearchProgressData GetProgress(string researchId)
int GetLevel(string researchId)
float GetRemainingTime(string researchId)

// Research Management
bool IsResearchAvailable(string researchId)
bool CanStartResearch(string researchId)
bool TryStartResearch(string researchId)
bool TryCompleteResearch(string researchId)

// Speed Up / Instant Complete
bool CanSpeedUpResearch(string researchId, float seconds)
bool TrySpeedUpResearch(string researchId, float seconds)
bool CanInstantCompleteResearch(string researchId)
bool TryInstantCompleteResearch(string researchId)

// Utility
int GetActiveResearchCount()
IEnumerable<ResearchProgressData> GetActiveResearch()
```

**Events:**
```csharp
event Action<string> ResearchStarted;
event Action<string, int> ResearchCompleted;
event Action<string> ResearchSpedUp;
```

### 4. UI Components

#### ResearchCardView
Location: `Code/Scripts/UI/GlobalUI/Research/ResearchCardView.cs`

Displays individual research item with:
- Icon and name
- Description
- Current/max level
- Current and next values (for BaseStat type)
- Time remaining (if researching)
- Progress bar
- Cost display
- Action buttons (Start, Speed Up, Instant Complete)

**Key Methods:**
```csharp
void Bind(string researchId)
void RefreshDisplay()
void OnActionButtonClicked()
void OnSpeedUpButtonClicked()
void OnInstantCompleteButtonClicked()
```

#### ResearchPanelUI
Location: `Code/Scripts/UI/GlobalUI/Research/ResearchPanelUI.cs`

Main panel managing research display:
- Research card container with scrolling
- Type filters (All, TowerBase, Turret, Projectile, BaseStat)
- Active research count display
- Currency balance display
- Auto-refresh on research events

**Key Methods:**
```csharp
void RefreshUI()
void SetFilter(ResearchType? filter)
```

## Data Flow

### Starting Research
1. Player clicks "Start Research" on ResearchCardView
2. `ResearchCardView.OnActionButtonClicked()` calls `ResearchService.TryStartResearch()`
3. ResearchService validates:
   - Research is available (prerequisites met)
   - Not already researching this item
   - Concurrent research limit not reached
   - Player has enough Cores
4. If valid:
   - Deducts Cores from PlayerManager
   - Creates/updates ResearchProgressData
   - Sets `isResearching = true`
   - Records start time (UTC ISO 8601)
   - Saves player data
   - Fires `ResearchStarted` event
5. EventManager broadcasts event
6. NotificationManager shows notification

### Research Completion
1. ResearchService.Update() checks active research
2. For each active research, calculates remaining time
3. When remaining time ≤ 0.1s:
   - Increments currentLevel
   - Clears research state
   - Saves player data
   - Fires `ResearchCompleted` event
   - Applies effects (unlocks or stats)
   - Shows notification

### Speed Up
1. Player clicks "Speed Up" button
2. Validates:
   - Research is active
   - Remaining time > 0
   - Player has enough Loops
3. If valid:
   - Deducts Loops
   - Adjusts start time forward by speedup amount
   - Saves data
   - Fires `ResearchSpedUp` event
   - Checks if now complete

### Instant Complete
1. Player clicks "Instant Complete" button
2. Validates:
   - Research is active
   - Player has enough Prisms (based on remaining time)
3. If valid:
   - Deducts Prisms
   - Adjusts start time to force completion
   - Saves data
   - Triggers normal completion flow

## Integration Points

### PlayerData
Added to PlayerData.cs:
```csharp
[Header("Research System")]
public ResearchSystemConfig researchConfig = new ResearchSystemConfig();
public List<ResearchProgressData> researchProgress = new List<ResearchProgressData>();
```

### PlayerManager
Added methods:
```csharp
public ResearchSystemConfig GetResearchConfig()
public List<ResearchProgressData> GetResearchProgress()
```

### EventNames
Added constants:
```csharp
public const string ResearchStarted = "ResearchStarted";
public const string ResearchCompleted = "ResearchCompleted";
public const string ResearchSpedUp = "ResearchSpedUp";
```

### NotificationSource
Already included `Research` enum value in NotificationData.cs

### OptionsUIManager
Added research panel management:
```csharp
[SerializeField] private GameObject researchPanel;
[SerializeField] private ResearchPanelUI researchPanelUI;

public void ShowResearch()
```

## Stat Contribution

For `ResearchType.BaseStat` research, the service implements `IStatContributor`:

```csharp
public void Contribute(StatCollector collector)
{
    foreach (var progress in researchProgress)
    {
        if (progress.currentLevel <= 0) continue;
        
        var def = GetDefinition(progress.researchId);
        if (def.researchType != ResearchType.BaseStat) continue;
        
        foreach (var bonus in def.statBonuses)
        {
            float scaledValue = bonus.value * progress.currentLevel;
            collector.Add[Kind](bonus.stat, scaledValue);
        }
    }
}
```

This integrates with the existing TowerStatPipeline system.

## Time Management

### Time Storage
Times are stored as ISO 8601 UTC strings for:
- Cross-platform compatibility
- Offline progression support
- Time zone handling

### Time Calculation
```csharp
public float GetRemainingTime(string researchId)
{
    var startTime = DateTime.Parse(progress.startTimeIsoUtc);
    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
    var remaining = progress.durationSeconds - elapsed;
    return Mathf.Max(0f, (float)remaining);
}
```

### Offline Progress
When player returns after being offline:
1. ResearchService.Update() checks active research
2. Calculates elapsed time since start
3. If duration exceeded, completes research automatically
4. Player receives notification of completion

## Progression Curves

### Exponential Growth Formula
```
value = base × factor^(level - 1)
```

Example (base=100, factor=1.5):
- Level 1: 100
- Level 5: 506
- Level 10: 3,844
- Level 20: 477,367

This creates the characteristic idle game "wall" that encourages long-term play.

### Custom Curves
Animation curves allow complete control:
```csharp
if (customTimeCurve != null && customTimeCurve.length > 0)
{
    float t = Mathf.Clamp01(level / (float)maxLevel);
    return baseTimeSeconds * customTimeCurve.Evaluate(t);
}
```

## Performance Considerations

1. **Update Loop**: Checks active research each frame but only processes time calculations for active items
2. **Card Pooling**: ResearchPanelUI reuses card instances rather than destroying/creating
3. **Event-Driven UI**: UI only refreshes on research events, not every frame
4. **Lazy Initialization**: Progress data created only when needed
5. **Indexed Definitions**: Dictionary lookup O(1) for definitions

## Testing Considerations

For testing, you can:
1. Create test research with short times (e.g., 10 seconds)
2. Use the instant complete function to skip waiting
3. Manually adjust `startTimeIsoUtc` in PlayerData for testing completion
4. Set `maxConcurrentResearch` to higher values for testing multiple research

## Future Enhancements

Potential additions:
1. Research queue system
2. Research slots (unlock more concurrent research)
3. Research categories with bonuses
4. Research tree visualization
5. Batch speed-up (speed up all active research)
6. Research presets (save/load research priorities)
7. Notification scheduling for research completion
8. Research statistics tracking
