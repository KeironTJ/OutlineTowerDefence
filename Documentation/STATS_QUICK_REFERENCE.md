# Stats Enrichment - Quick Reference

## What Players Can Now See

### Round Summary (Live & Historical)
```
Round Summary
├── Duration: 05:23
├── Difficulty: 5
├── Highest Wave: 28
└── Tower Base: BASE_001

Combat Stats
├── Bullets Fired: 1,234
├── Total Damage: 45,678
└── Critical Hits: 89

Projectile Usage
├── STD_BULLET
│   ├── Shots: 450
│   └── Damage: 12,345
├── PIERCE_SHOT
│   ├── Shots: 380
│   └── Damage: 18,900
└── EXPLOSIVE_ROUND
    ├── Shots: 404
    └── Damage: 14,433

Turret Usage
├── STD: 450 shots
├── RAPID: 380 shots
└── SNIPER: 404 shots

Currency Earned
├── Fragments: 15,234
├── Cores: 234
├── Prisms: 12
└── Loops: 2

Enemies Destroyed: 567
├── Basic (450)
│   ├── GRUNT: 200
│   ├── RUNNER: 150
│   └── TANK: 100
├── Elite (100)
│   ├── SHIELD: 50
│   └── HEAVY: 50
└── Boss (17)
    └── MEGA: 17
```

### Player Lifetime Stats
```
Currency
├── Fragments Earned: 1.2M
├── Cores Earned: 45.3K
├── Prisms Earned: 2.1K
└── Loops Earned: 156

Round Stats
├── Total Rounds Completed: 456
└── Total Waves Completed: 8,234

Combat Stats
├── Total Damage Dealt: 2.3M
└── Critical Hits: 12.4K

Projectile Usage (Lifetime)
├── STD_BULLET
│   ├── Shots: 125K
│   └── Damage: 890K
├── PIERCE_SHOT
│   ├── Shots: 98K
│   └── Damage: 1.2M
└── [Top 10 shown]

Turret Usage (Lifetime)
├── STD: 125K shots
├── RAPID: 98K shots
└── [Top 10 shown]

Difficulty
├── Level 0: 150
├── Level 1: 145
├── Level 2: 132
└── [etc...]

Enemies Killed
└── [Same hierarchical structure as round stats]
```

## Key Benefits

1. **Performance Analysis** - See which weapons/projectiles work best
2. **Progress Tracking** - Watch improvement over time
3. **Strategic Planning** - Identify effective combinations
4. **Achievement Hunting** - Track towards stat-based goals
5. **Engagement** - More data = more reasons to play

## Data Hierarchy

```
RoundRecord (per round)
    ├── Basic Info (time, difficulty, wave)
    ├── Tower & Turrets (what was used)
    ├── Combat Stats (damage, crits, bullets)
    ├── Projectile Details (per type)
    ├── Currency Earned
    └── Enemy Kills (per type/tier/family)

PlayerData (lifetime)
    ├── Accumulated Combat Stats
    ├── Accumulated Projectile Stats
    ├── Accumulated Turret Stats
    ├── Accumulated Enemy Kills
    ├── Round History (all RoundRecords)
    └── Other Progress Metrics
```

## Technical Flow

```
Gameplay → Events → RoundManager → RoundRecord → PlayerManager → PlayerData
                                        ↓
                                    UI Panels
                                (Live & Historical)
```

## Example Use Cases

### "Which projectile should I upgrade?"
→ Check lifetime stats to see which deals most damage

### "Am I getting better at higher difficulties?"
→ Compare recent rounds to older ones in history

### "What's my best turret combo?"
→ See which turrets fire most in successful rounds

### "How effective are critical hits?"
→ Track crit rate and compare damage output

### "Which enemies give me trouble?"
→ See kill counts to identify weak spots in strategy
