using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventNames
{
    // Tower Events
    public const string TowerSpawned = "TowerSpawned";
    public const string TowerDestroyed = "TowerDestroyed";
    public const string BulletFired = "BulletFired";
    public const string DamageDealt = "DamageDealt";

    // Round Specific Events
    public const string RoundStatsUpdated = "RoundStatsUpdated";
    public const string RoundStarted = "RoundStarted";
    public const string RoundRecordCreated = "RoundRecordCreated";
    public const string RoundRecordUpdated = "RoundRecordUpdated";
    public const string RoundEnded = "RoundEnded";
    public const string RoundCompleted = "RoundCompleted";

    // Wave Events
    public const string NewWaveStarted = "NewWaveStarted";
    public const string WaveCompleted = "WaveCompleted";

    // Difficulty Events
    public const string DifficultyAchieved = "DifficultyAchieved";

    // Enemy Events
    public const string EnemySpawned = "EnemySpawned";
    public const string EnemyDestroyed = "EnemyDestroyed";
    public const string RawEnemyRewardEvent = "EnemyRawReward";
    public const string EnemyDestroyedDefinition = "EnemyDestroyedDefinition";

    // Game Currency Event
    public const string CurrencyEarned = "CurrencyEarned";
    public const string CurrencySpent = "CurrencySpent";

    // Skill Events
    public const string SkillUnlocked = "SkillUnlocked";
    public const string SkillUpgraded = "SkillUpgraded";

    // Achievement Events
    public const string AchievementTierCompleted = "AchievementTierCompleted";

}
