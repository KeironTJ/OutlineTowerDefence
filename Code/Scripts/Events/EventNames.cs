using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EventNames
{
    // Tower Events
    public const string TowerSpawned = "TowerSpawned";
    public const string TowerDestroyed = "TowerDestroyed";

    // Enemy Events
    public const string EnemySpawned = "EnemySpawned";
    public const string EnemyDestroyed = "EnemyDestroyed";

    // Enum for Currency Types
    public enum CurrencyType
    {
        Basic,
        Premium,
        Special,
        Luxury
    }

    // Game Currency Event
    public const string CreditsEarned = "CreditsEarned";

}
