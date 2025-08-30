using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventNames
{
    // Tower Events
    public const string TowerSpawned = "TowerSpawned";
    public const string TowerDestroyed = "TowerDestroyed";
    public const string BulletFired = "BulletFired";

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

    // Enemy Events
    public const string EnemySpawned = "EnemySpawned";
    public const string EnemyDestroyed = "EnemyDestroyed";

    // Game Currency Event
    public const string CreditsEarned = "CreditsEarned";
    public const string CreditsSpent = "CreditsSpent";

    // Skill Events
    public const string SkillUnlocked = "SkillUnlocked";
    public const string SkillUpgraded = "SkillUpgraded";

}
