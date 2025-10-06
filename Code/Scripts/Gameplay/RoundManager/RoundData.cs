using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoundRecord
{
    public string id;
    public string startedAtIsoUtc;
    public string endedAtIsoUtc;
    public float durationSeconds;
    public int difficulty;
    public int highestWave;
    public int bulletsFired;
    public int enemiesKilled;

    public List<CurrencyAmount> currencyEarned = new List<CurrencyAmount>();

    // New: per-definition kill summary (replaces enemyBreakdown)
    public List<EnemyKillSummary> enemyKills = new List<EnemyKillSummary>();

    // Optional aggregated views
    public List<TierKillSummary> tierKills = new List<TierKillSummary>();
    public List<FamilyKillSummary> familyKills = new List<FamilyKillSummary>();
    
    // Tower & Turret stats
    public string towerBaseId;
    public List<TurretUsageSummary> turretUsage = new List<TurretUsageSummary>();
    
    // Projectile stats
    public List<ProjectileUsageSummary> projectileUsage = new List<ProjectileUsageSummary>();
    
    // Damage stats
    public float totalDamageDealt;
    public int criticalHits;
}

[Serializable] public struct CurrencyAmount { public CurrencyType type; public float amount; }

[Serializable]
public struct EnemyKillSummary
{
    public string definitionId;
    public EnemyTier tier;
    public string family;
    public EnemyTrait traits;
    public int count;
}

[Serializable]
public struct TierKillSummary
{
    public EnemyTier tier;
    public int count;
}

[Serializable]
public struct FamilyKillSummary
{
    public string family;
    public int count;
}

[Serializable]
public struct TurretUsageSummary
{
    public string turretId;
    public int shotsFired;
}

[Serializable]
public struct ProjectileUsageSummary
{
    public string projectileId;
    public int shotsFired;
    public float damageDealt;
}

public static class RoundDataConverters
{
    public static List<CurrencyAmount> ToCurrencyList(Dictionary<CurrencyType, float> dict)
    {
        var list = new List<CurrencyAmount>();
        if (dict == null) return list;
        foreach (var kv in dict)
            list.Add(new CurrencyAmount { type = kv.Key, amount = kv.Value });
        return list;
    }

    // From runtime dictionary<string,int> plus a lookup of definitions
    public static List<EnemyKillSummary> ToEnemyKillSummaries(
        Dictionary<string, int> killsByDefinition,
        IReadOnlyDictionary<string, EnemyTypeDefinition> defLookup)
    {
        var list = new List<EnemyKillSummary>();
        if (killsByDefinition == null) return list;

        foreach (var kv in killsByDefinition)
        {
            EnemyTier tier = EnemyTier.Basic;
            string family = "Unknown";
            EnemyTrait traits = EnemyTrait.None;

            if (defLookup != null && defLookup.TryGetValue(kv.Key, out var def) && def)
            {
                tier = def.tier;
                family = def.family;
                traits = def.traits;
            }

            list.Add(new EnemyKillSummary
            {
                definitionId = kv.Key,
                tier = tier,
                family = family,
                traits = traits,
                count = kv.Value
            });
        }
        return list;
    }

    public static List<TierKillSummary> AggregateTierKills(List<EnemyKillSummary> kills)
    {
        var dict = new Dictionary<EnemyTier, int>();
        foreach (var k in kills)
        {
            if (dict.TryGetValue(k.tier, out var c)) dict[k.tier] = c + k.count;
            else dict[k.tier] = k.count;
        }
        var list = new List<TierKillSummary>();
        foreach (var kv in dict)
            list.Add(new TierKillSummary { tier = kv.Key, count = kv.Value });
        return list;
    }

    public static List<FamilyKillSummary> AggregateFamilyKills(List<EnemyKillSummary> kills)
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var k in kills)
        {
            var key = k.family ?? "Unknown";
            if (dict.TryGetValue(key, out var c)) dict[key] = c + k.count;
            else dict[key] = k.count;
        }
        var list = new List<FamilyKillSummary>();
        foreach (var kv in dict)
            list.Add(new FamilyKillSummary { family = kv.Key, count = kv.Value });
        return list;
    }
    
    public static List<TurretUsageSummary> ToTurretUsageSummaries(Dictionary<string, int> shotsByTurret)
    {
        var list = new List<TurretUsageSummary>();
        if (shotsByTurret == null) return list;
        
        foreach (var kv in shotsByTurret)
        {
            list.Add(new TurretUsageSummary
            {
                turretId = kv.Key,
                shotsFired = kv.Value
            });
        }
        return list;
    }
    
    public static List<ProjectileUsageSummary> ToProjectileUsageSummaries(
        Dictionary<string, int> shotsByProjectile, 
        Dictionary<string, float> damageByProjectile)
    {
        var list = new List<ProjectileUsageSummary>();
        if (shotsByProjectile == null) return list;
        
        foreach (var kv in shotsByProjectile)
        {
            float damage = 0f;
            if (damageByProjectile != null)
                damageByProjectile.TryGetValue(kv.Key, out damage);
            
            list.Add(new ProjectileUsageSummary
            {
                projectileId = kv.Key,
                shotsFired = kv.Value,
                damageDealt = damage
            });
        }
        return list;
    }
}

// (Legacy MonoBehaviour was unused—safe to remove. Keep only if you still reference it.)
public class RoundData : MonoBehaviour { }
