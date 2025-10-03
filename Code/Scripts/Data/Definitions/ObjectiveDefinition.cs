using UnityEngine;

public enum ObjectiveType { KillEnemies, CompleteRounds, CompleteWaves, SpendCurrency, UnlockSkill, EarnCurrency, CompleteDailyObjectives, CompleteWeeklyObjectives }
public enum ObjectiveRarity { Common, Uncommon, Rare, Epic }
public enum ObjectivePeriod { Daily, Weekly }

[CreateAssetMenu(menuName = "Rewards/Objective")]
public class ObjectiveDefinition : ScriptableObject
{
    [Header("Identity / Classification")]
    public string id;
    public ObjectivePeriod period = ObjectivePeriod.Daily;
    public ObjectiveType type;
    public ObjectiveRarity rarity = ObjectiveRarity.Common;

    [Header("Goal")]
    public float targetAmount = 1f;

    [Header("Enemy Filters (KillEnemies)")]
    public bool anyEnemy = true;                 // if true ignores all below
    public string targetDefinitionId;            // exact definition id (optional)
    public bool useTierFilter = false;
    public EnemyTier targetTier = EnemyTier.Basic;
    public bool useFamilyFilter = false;
    public string targetFamily;
    public bool useTraitFilter = false;
    public EnemyTrait targetTraits = EnemyTrait.None;  // all required bits

    [Header("Currency Filter (Earn/Spend)")]
    public CurrencyType currencyType = CurrencyType.Fragments;

    [Header("Skill Filter (UnlockSkill)")]
    public string skillId;

    [Header("Reward")]
    public CurrencyType rewardType;
    public int rewardAmount = 1000;
    public bool manualClaim = true;

    [Header("UI")]
    [TextArea] public string description;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name;

        if (targetAmount < 1) targetAmount = 1;

        if (type != ObjectiveType.KillEnemies)
        {
            anyEnemy = true;
            targetDefinitionId = string.Empty;
            useTierFilter = useFamilyFilter = useTraitFilter = false;
        }

        if (type != ObjectiveType.EarnCurrency && type != ObjectiveType.SpendCurrency)
            currencyType = CurrencyType.Cores;

        if (type != ObjectiveType.UnlockSkill)
            skillId = string.IsNullOrWhiteSpace(skillId) ? "" : skillId.Trim();
    }

    [Header("Debug")]
    public bool debugMatch;
#endif

    public bool Matches(EnemyDestroyedDefinitionEvent e)
    {
        if (type != ObjectiveType.KillEnemies) return false;
        if (anyEnemy) return true;

        if (!string.IsNullOrEmpty(targetDefinitionId) && e.definitionId != targetDefinitionId)
            return false;
        if (useTierFilter && e.tier != targetTier)
            return false;
        if (useFamilyFilter && e.family != targetFamily)
            return false;
        if (useTraitFilter && (e.traits & targetTraits) != targetTraits)
            return false;

        return true;
    }

    public bool MatchesDetailed(EnemyDestroyedDefinitionEvent e, out string reason)
    {
        reason = "OK";
        if (type != ObjectiveType.KillEnemies) { reason = "Type!=KillEnemies"; return false; }
        if (anyEnemy) return true;

        if (!string.IsNullOrEmpty(targetDefinitionId) && e.definitionId != targetDefinitionId)
        { reason = $"definition mismatch ({targetDefinitionId} vs {e.definitionId})"; return false; }

        if (useTierFilter && e.tier != targetTier)
        { reason = $"tier mismatch ({targetTier} vs {e.tier})"; return false; }

        if (useFamilyFilter && e.family != targetFamily)
        { reason = $"family mismatch ({targetFamily} vs {e.family})"; return false; }

        if (useTraitFilter && (e.traits & targetTraits) != targetTraits)
        { reason = $"traits mismatch (need {targetTraits} have {e.traits})"; return false; }

        return true;
    }
}