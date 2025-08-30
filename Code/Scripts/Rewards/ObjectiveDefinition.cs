using UnityEngine;

public enum ObjectiveType { KillEnemies, CompleteRounds, CompleteWaves, SpendCredits, UnlockSkill, EarnCredits }
public enum ObjectiveDifficulty { Easy, Medium, Hard }
public enum ObjectivePeriod { Daily, Weekly }

[CreateAssetMenu(menuName = "Rewards/Objective")]
public class ObjectiveDefinition : ScriptableObject
{
    [Header("Identity / Classification")]
    public string id;
    public ObjectivePeriod period = ObjectivePeriod.Daily;
    public ObjectiveType type;
    public ObjectiveDifficulty difficulty = ObjectiveDifficulty.Easy;

    [Header("Goal")]
    public float targetAmount = 1f;

    [Header("Enemy Filters (used when type == KillEnemies)")]
    [SerializeField] public bool anyEnemyType = true;
    [SerializeField] public EnemyType enemyType;              // Only used if anyEnemyType == false
    [SerializeField] public bool anyEnemySubtype = true;
    [SerializeField] public EnemySubtype enemySubtype;        // Only used if anyEnemySubtype == false

    [Header("Credit Filter (used when type == EarnCredits or SpendCredits)")]
    public CurrencyType creditCurrencyType = CurrencyType.Basic;

    [Header("Skill Filter (used when type == UnlockSkill)")]
    public string skillId;                   // Leave blank for any skill (or enforce exact match)

    [Header("Reward")]
    public CurrencyType rewardType;
    public int rewardAmount = 10;
    public bool manualClaim = true;

    [Header("UI")]
    [TextArea] public string description;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name;
        if (targetAmount < 1) targetAmount = 1;

        // Auto-sanitize unrelated filters
        if (type != ObjectiveType.KillEnemies)
        {
            anyEnemyType = false;
            anyEnemySubtype = false;
        }
        if (type != ObjectiveType.EarnCredits && type != ObjectiveType.SpendCredits)
        {
            creditCurrencyType = CurrencyType.Basic;
        }
        if (type != ObjectiveType.UnlockSkill)
        {
            if (!string.IsNullOrEmpty(skillId))
                skillId = skillId.Trim();
        }
    }
#endif
}