using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    private readonly Dictionary<GameObject, float> active = new Dictionary<GameObject, float>();

    public event Action OnThreatChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterEnemy(GameObject enemyGO, float threatValue)
    {
        if (enemyGO == null) return;
        active[enemyGO] = Mathf.Max(0f, threatValue);
        OnThreatChanged?.Invoke();
    }

    public void UnregisterEnemy(GameObject enemyGO)
    {
        if (enemyGO == null) return;
        if (active.Remove(enemyGO))
            OnThreatChanged?.Invoke();
    }

    public float GetActiveThreat()
    {
        float sum = 0f;
        var keys = new List<GameObject>(active.Keys);
        foreach (var k in keys)
        {
            if (k == null)
            {
                active.Remove(k);
                continue;
            }
            sum += active[k];
        }
        return sum;
    }

    public int GetActiveCount() => active.Count;
}