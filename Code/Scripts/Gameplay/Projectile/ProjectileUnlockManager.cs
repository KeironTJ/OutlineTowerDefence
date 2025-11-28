using System.Collections.Generic;
using UnityEngine;

public class ProjectileUnlockManager : SingletonMonoBehaviour<ProjectileUnlockManager>
{
    [Tooltip("Assign in Inspector, or place assets under Resources/Data/ProjectileUnlocks to auto-load")]
    [SerializeField] private List<ProjectileUnlockDefinition> defs = new List<ProjectileUnlockDefinition>();

    private readonly Dictionary<string, ProjectileUnlockDefinition> byProjectileId = new Dictionary<string, ProjectileUnlockDefinition>();

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

    protected override void OnAwakeAfterInit()
    {
        DefinitionLoader.LoadAndMerge(ref defs, "Data/ProjectileUnlocks", def => def.projectileId);
        byProjectileId.Clear();
        foreach (var d in defs)
        {
            if (d == null || string.IsNullOrEmpty(d.projectileId)) continue;
            if (!byProjectileId.ContainsKey(d.projectileId)) byProjectileId[d.projectileId] = d;
        }

        // Ensure default-granted projectiles are unlocked once
        var pm = PlayerManager.main;
        if (pm?.playerData != null)
        {
            foreach (var d in defs)
                if (d != null && d.grantByDefault && !IsUnlocked(pm, d.projectileId))
                    UnlockFree(pm, d.projectileId);
        }
    }

    public bool IsUnlocked(PlayerManager pm, string projectileId)
    {
        return pm?.playerData?.unlockedProjectileIds != null &&
               pm.playerData.unlockedProjectileIds.Contains(projectileId);
    }

    public bool CanUnlock(PlayerManager pm, string projectileId, out string reason, out CurrencyCost cost)
    {
        reason = "";
        cost = default;
        if (pm?.playerData == null) { reason = "No player"; return false; }

        if (IsUnlocked(pm, projectileId)) { reason = "Already unlocked"; return false; }

        if (!byProjectileId.TryGetValue(projectileId, out var d) || d == null)
        {
            reason = "No unlock data";
            return false;
        }

        // prerequisites
        if (d.prerequisiteProjectileIds != null)
            foreach (var pre in d.prerequisiteProjectileIds)
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

    public bool TryUnlock(PlayerManager pm, string projectileId, out string failReason)
    {
        failReason = "";
        if (!byProjectileId.TryGetValue(projectileId, out var d) || d == null) { failReason = "No unlock data"; return false; }

        if (!CanUnlock(pm, projectileId, out failReason, out var cost)) return false;

        // spend and grant
        pm.playerData.cores  -= cost.cores;
        pm.playerData.prisms -= cost.prisms;
        pm.playerData.loops  -= cost.loops;

        if (pm.playerData.unlockedProjectileIds == null) pm.playerData.unlockedProjectileIds = new List<string>();
        pm.playerData.unlockedProjectileIds.Add(projectileId);
        pm.SavePlayerData();

        return true;
    }

    private void UnlockFree(PlayerManager pm, string projectileId)
    {
        if (pm.playerData.unlockedProjectileIds == null) pm.playerData.unlockedProjectileIds = new List<string>();
        if (!pm.playerData.unlockedProjectileIds.Contains(projectileId))
        {
            pm.playerData.unlockedProjectileIds.Add(projectileId);
            pm.SavePlayerData();
        }
    }
}
