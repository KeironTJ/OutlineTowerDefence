using System.Collections.Generic;
using UnityEngine;

public class TurretUnlockManager : MonoBehaviour
{
    public static TurretUnlockManager Instance;

    [Tooltip("Assign in Inspector, or place assets under Resources/Data/TurretUnlocks to auto-load")]
    [SerializeField] private List<TurretUnlockDefinition> defs = new List<TurretUnlockDefinition>();

    private readonly Dictionary<string, TurretUnlockDefinition> byTurretId = new Dictionary<string, TurretUnlockDefinition>();

    [System.Serializable]
    public struct CurrencyCost
    {
        public int cores, prisms, loops;
        public string ToLabel()
        {
            List<string> p = new List<string>(3);
            if (cores > 0) p.Add($"{cores} C");
            if (prisms > 0) p.Add($"{prisms} P");
            if (loops > 0) p.Add($"{loops} L");
            return p.Count == 0 ? "Free" : string.Join(" • ", p);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (defs == null || defs.Count == 0)
        {
            var loaded = Resources.LoadAll<TurretUnlockDefinition>("Data/TurretUnlocks");
            if (loaded != null && loaded.Length > 0) defs = new List<TurretUnlockDefinition>(loaded);
        }
        RebuildMap();

        // Ensure default-granted turrets are unlocked once
        var pm = PlayerManager.main;
        if (pm?.playerData != null)
        {
            foreach (var d in defs)
                if (d != null && d.grantByDefault && !IsUnlocked(pm, d.turretId))
                    UnlockFree(pm, d.turretId);
        }
    }

    private void RebuildMap()
    {
        byTurretId.Clear();
        foreach (var d in defs)
        {
            if (d == null || string.IsNullOrEmpty(d.turretId)) continue;
            if (!byTurretId.ContainsKey(d.turretId)) byTurretId[d.turretId] = d;
        }
    }

    public bool IsUnlocked(PlayerManager pm, string turretId)
    {
        return pm?.playerData?.unlockedTurretIds != null &&
               pm.playerData.unlockedTurretIds.Contains(turretId);
    }

    public bool CanUnlock(PlayerManager pm, string turretId, out string reason, out CurrencyCost cost)
    {
        reason = "";
        cost = default;
        if (pm?.playerData == null) { reason = "No player"; return false; }

        if (IsUnlocked(pm, turretId)) { reason = "Already unlocked"; return false; }

        if (!byTurretId.TryGetValue(turretId, out var d) || d == null)
        {
            reason = "No unlock data";
            return false;
        }

        // prerequisites
        if (d.prerequisiteTurretIds != null)
            foreach (var pre in d.prerequisiteTurretIds)
                if (!string.IsNullOrEmpty(pre) && !IsUnlocked(pm, pre))
                {
                    reason = $"Requires {pre}";
                    return false;
                }

        // progression
        int highestWave = pm.GetHighestWave(Mathf.Max(1, pm.GetDifficulty())); // or any difficulty
        if (highestWave < d.requiredHighestWave)
        {
            reason = d.lockedHint != "" ? d.lockedHint : $"Reach wave {d.requiredHighestWave}";
            return false;
        }
        if (pm.GetMaxDifficulty() < d.requiredMaxDifficulty)
        {
            reason = d.lockedHint != "" ? d.lockedHint : $"Reach difficulty {d.requiredMaxDifficulty}";
            return false;
        }

        // cost
        cost = new CurrencyCost { cores = d.costCores, prisms = d.costPrisms, loops = d.costLoops };

        // affordability
        var pd = pm.playerData;
        if (pd.cores < cost.cores || pd.prisms < cost.prisms || pd.loops < cost.loops)
        {
            reason = $"Need {cost.ToLabel()}";
            return false;
        }

        reason = "Available";
        return true;
    }

    public bool TryUnlock(PlayerManager pm, string turretId, out string failReason)
    {
        failReason = "";
        if (!byTurretId.TryGetValue(turretId, out var d) || d == null) { failReason = "No unlock data"; return false; }

        if (!CanUnlock(pm, turretId, out failReason, out var cost)) return false;

        // spend and grant
        pm.playerData.cores  -= cost.cores;
        pm.playerData.prisms -= cost.prisms;
        pm.playerData.loops  -= cost.loops;

        if (pm.playerData.unlockedTurretIds == null) pm.playerData.unlockedTurretIds = new List<string>();
        pm.playerData.unlockedTurretIds.Add(turretId);
        pm.SavePlayerData();

        return true;
    }

    private void UnlockFree(PlayerManager pm, string turretId)
    {
        if (pm.playerData.unlockedTurretIds == null) pm.playerData.unlockedTurretIds = new List<string>();
        if (!pm.playerData.unlockedTurretIds.Contains(turretId))
        {
            pm.playerData.unlockedTurretIds.Add(turretId);
            pm.SavePlayerData();
        }
    }
}