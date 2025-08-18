using System;
using System.Collections;
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

    public List<CurrencyAmount> creditsEarned = new List<CurrencyAmount>();
    public List<EnemyTypeCount> enemyBreakdown = new List<EnemyTypeCount>();
}

[Serializable] public struct CurrencyAmount { public CurrencyType type; public float amount; }
[Serializable] public class EnemySubtypeCount { public EnemySubtype subtype; public int count; }
[Serializable] public class EnemyTypeCount { public EnemyType type; public int total; public List<EnemySubtypeCount> subtypes = new List<EnemySubtypeCount>(); }

public static class RoundDataConverters
{
    public static List<CurrencyAmount> ToCurrencyList(Dictionary<CurrencyType, float> dict)
    {
        var list = new List<CurrencyAmount>();
        if (dict == null) return list;
        foreach (var kv in dict) list.Add(new CurrencyAmount { type = kv.Key, amount = kv.Value });
        return list;
    }

    public static List<EnemyTypeCount> ToEnemyBreakdown(Dictionary<EnemyType, Dictionary<EnemySubtype, int>> nested)
    {
        var result = new List<EnemyTypeCount>();
        if (nested == null) return result;

        foreach (var typeEntry in nested)
        {
            var etc = new EnemyTypeCount { type = typeEntry.Key, total = 0, subtypes = new List<EnemySubtypeCount>() };
            var subDict = typeEntry.Value;
            if (subDict != null)
            {
                foreach (var sub in subDict)
                {
                    if (sub.Value <= 0) continue;
                    etc.subtypes.Add(new EnemySubtypeCount { subtype = sub.Key, count = sub.Value });
                    etc.total += sub.Value;
                }
            }
            result.Add(etc);
        }
        return result;
    }
}

public class RoundData : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
