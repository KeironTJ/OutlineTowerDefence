using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [SerializeField] private RoundSkillData roundSkillData = new RoundSkillData();

    [SerializeField] private List<Skill> debugAttackSkills = new List<Skill>();
    [SerializeField] private List<Skill> debugDefenceSkills = new List<Skill>();
    [SerializeField] private List<Skill> debugSupportSkills = new List<Skill>();
    [SerializeField] private List<Skill> debugSpecialSkills = new List<Skill>();

    public void InitializeSkills(Dictionary<string, Skill> playerAttackSkills, Dictionary<string, Skill> playerDefenceSkills,
                                 Dictionary<string, Skill> playerSupportSkills, Dictionary<string, Skill> playerSpecialSkills)
    {
        roundSkillData.attackSkills = CloneSkills(playerAttackSkills);
        roundSkillData.defenceSkills = CloneSkills(playerDefenceSkills);
        roundSkillData.supportSkills = CloneSkills(playerSupportSkills);
        roundSkillData.specialSkills = CloneSkills(playerSpecialSkills);

        // Populate debugging lists
        debugAttackSkills = new List<Skill>(roundSkillData.attackSkills.Values);
        debugDefenceSkills = new List<Skill>(roundSkillData.defenceSkills.Values);
        debugSupportSkills = new List<Skill>(roundSkillData.supportSkills.Values);
        debugSpecialSkills = new List<Skill>(roundSkillData.specialSkills.Values);

        // Log checks to ensure initialization
        //Debug.Log($"Attack Skills Initialized: {debugAttackSkills.Count} skills");
        //Debug.Log($"Defence Skills Initialized: {debugDefenceSkills.Count} skills");
        //Debug.Log($"Support Skills Initialized: {debugSupportSkills.Count} skills");
        //Debug.Log($"Special Skills Initialized: {debugSpecialSkills.Count} skills");

        if (debugAttackSkills.Count == 0 || debugDefenceSkills.Count == 0 || debugSupportSkills.Count == 0 || debugSpecialSkills.Count == 0)
        {
            Debug.LogWarning("One or more skill categories are empty after initialization.");
        }
    }

    private Dictionary<string, Skill> CloneSkills(Dictionary<string, Skill> originalSkills)
    {
        Dictionary<string, Skill> clonedSkills = new Dictionary<string, Skill>();
        foreach (var kvp in originalSkills)
        {
            clonedSkills[kvp.Key] = PlayerManager.CloneScriptableObject(kvp.Value);
        }
        return clonedSkills;
    }

    public Skill GetSkill(string skillName)
    {
        if (roundSkillData.attackSkills.TryGetValue(skillName, out Skill attackSkill))
        {
            return attackSkill;
        }
        if (roundSkillData.defenceSkills.TryGetValue(skillName, out Skill defenceSkill))
        {
            return defenceSkill;
        }
        if (roundSkillData.supportSkills.TryGetValue(skillName, out Skill supportSkill))
        {
            return supportSkill;
        }
        if (roundSkillData.specialSkills.TryGetValue(skillName, out Skill specialSkill))
        {
            return specialSkill;
        }

        Debug.LogWarning($"Skill not found: {skillName}");
        return null;
    }

    public float GetSkillValue(Skill skill)
    {
        return skill.baseValue * Mathf.Pow(skill.level, skill.upgradeModifier);
    }

    public void UpgradeSkill(Skill skill, int levelIncrease)
    {
        if (skill == null || skill.level == skill.maxLevel)
        {
            Debug.LogWarning($"{skill?.skillName} is already at Max Level or null.");
            return;
        }

        skill.level += levelIncrease;
    }

    public Dictionary<string, Skill> GetSkillsByCategory(string category)
    {
        return category switch
        {
            "Attack" => roundSkillData.attackSkills,
            "Defence" => roundSkillData.defenceSkills,
            "Support" => roundSkillData.supportSkills,
            "Special" => roundSkillData.specialSkills,
            _ => new Dictionary<string, Skill>()
        };
    }

    public float GetSkillCost(Skill skill)
    {
        if (skill == null) return 0f;

        float playerSkillLevel = PlayerManager.main.GetSkillLevel(PlayerManager.main.GetSkill(skill.skillName));
        float currentSkillRoundLevel = skill.level - playerSkillLevel + 1;
        return skill.basicCost * Mathf.Pow(currentSkillRoundLevel, skill.basicCostModifier);
    }
}
