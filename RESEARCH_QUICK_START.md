# Research System - Quick Start Guide

## Overview

The Research System allows players to unlock and upgrade various game elements over time using an idle game progression pattern. Research requires time and currency (Cores), with options to speed up using Loops or instantly complete using Prisms.

## Research Types

1. **Tower Base Research**: Unlocks new tower base designs
2. **Turret Research**: Unlocks new turret types
3. **Projectile Research**: Unlocks new projectile types
4. **Base Stat Research**: Provides permanent stat bonuses

## Getting Started

### 1. Creating a Research Definition

Create a new Research Definition asset:
```
Assets → Create → Game → Research Definition
```

Configure the research item:
- **ID**: Unique identifier (e.g., "RESEARCH_TOWER_BASE_01")
- **Display Name**: Player-facing name
- **Description**: What the research does
- **Icon**: Visual representation
- **Research Type**: Select the appropriate type
- **Max Level**: Maximum research level (default: 10)

### 2. Configure Time and Cost

**Time Configuration:**
- **Base Time Seconds**: Time for level 1 (e.g., 60 = 1 minute)
- **Time Growth Factor**: Exponential multiplier (e.g., 1.5 = 50% increase per level)
- **Time Curve**: Choose Linear, Exponential, Quadratic, or Custom

**Cost Configuration:**
- **Base Core Cost**: Cores cost for level 1
- **Core Cost Growth Factor**: Exponential multiplier for cost
- **Core Cost Curve**: Choose curve type

**Speed-Up and Instant Complete:**
- **Loops Per Hour Speedup**: Loops required per hour of speedup (default: 10)
- **Prisms Per Hour Instant**: Prisms cost for instant completion per hour (default: 50)

### 3. Set Prerequisites (Optional)

Add prerequisite research IDs that must be completed before this research becomes available.

### 4. Configure Effects

**For Unlock Types (Tower Base, Turret, Projectile):**
- Set **Unlock Target ID** to the item ID to unlock

**For Base Stat Research:**
- Add **Stat Bonuses** array entries
- Configure stat type, value, and contribution kind

### 5. Add to ResearchService

Add your Research Definition to the ResearchService component:
1. Find the ResearchService GameObject in your scene
2. Add your definition to the "Loaded Definitions" array

## Using the Research System

### In Code

```csharp
// Check if research is available
bool available = ResearchService.Instance.IsResearchAvailable("RESEARCH_TOWER_BASE_01");

// Start research
bool started = ResearchService.Instance.TryStartResearch("RESEARCH_TOWER_BASE_01");

// Check progress
int level = ResearchService.Instance.GetLevel("RESEARCH_TOWER_BASE_01");
float remaining = ResearchService.Instance.GetRemainingTime("RESEARCH_TOWER_BASE_01");

// Speed up research by 1 hour
bool speedUp = ResearchService.Instance.TrySpeedUpResearch("RESEARCH_TOWER_BASE_01", 3600f);

// Instant complete
bool completed = ResearchService.Instance.TryInstantCompleteResearch("RESEARCH_TOWER_BASE_01");
```

### In UI

Access the Research panel through the Options menu:
1. Open Options (ESC or Options button)
2. Click "Research" button
3. Browse available research items
4. Click "Start Research" on available items
5. Use Speed Up or Instant Complete buttons on active research

## Currency System

- **Cores** (Primary): Required to start research
- **Loops** (Secondary): Speed up active research
- **Prisms** (Tertiary): Instantly complete active research

## Progression Curves

### Linear
Cost/Time = Base + (Level - 1) × Growth

### Exponential (Recommended for Idle Games)
Cost/Time = Base × Growth^(Level - 1)

Example: Base 100, Growth 1.5
- Level 1: 100
- Level 2: 150
- Level 3: 225
- Level 4: 337.5
- Level 5: 506.25

### Quadratic
Cost/Time = Base × Level² × Growth

### Custom
Use an Animation Curve for complete control over progression

## Events

Listen to research events:
```csharp
ResearchService.Instance.ResearchStarted += OnResearchStarted;
ResearchService.Instance.ResearchCompleted += OnResearchCompleted;
ResearchService.Instance.ResearchSpedUp += OnResearchSpedUp;
```

Or use EventManager:
```csharp
EventManager.StartListening(EventNames.ResearchStarted, OnResearchStartedEvent);
EventManager.StartListening(EventNames.ResearchCompleted, OnResearchCompletedEvent);
```

## Tips

1. **Balance Time Scales**: Start with short times (minutes) for early levels, scaling to hours/days for late game
2. **Exponential Growth**: Use growth factors between 1.3 and 2.0 for good progression feel
3. **Prerequisites**: Create research chains to guide player progression
4. **Base Stats**: Use for long-term permanent progression alongside temporary upgrades
5. **Concurrent Research**: Adjust `maxConcurrentResearch` in ResearchSystemConfig to allow multiple simultaneous research projects (default: 1)

## Example Research Definitions

### Tower Base Unlock
- ID: "RESEARCH_TOWER_BASE_HEAVY"
- Type: TowerBase
- Base Time: 300 seconds (5 minutes)
- Time Growth: 1.5
- Base Cost: 500 cores
- Cost Growth: 1.5
- Max Level: 1
- Unlock Target: "0004" (Heavy tower base ID)

### Attack Damage Boost
- ID: "RESEARCH_ATTACK_DAMAGE"
- Type: BaseStat
- Base Time: 600 seconds (10 minutes)
- Time Growth: 1.4
- Base Cost: 1000 cores
- Cost Growth: 1.5
- Max Level: 10
- Stat Bonuses: [AttackDamage, FlatBonus, 5.0]
  - Each level grants +5 attack damage
