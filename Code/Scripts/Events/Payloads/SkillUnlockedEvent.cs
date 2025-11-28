using System;

[Serializable]
public struct SkillUnlockedEvent
{
    public string skillId;

    public SkillUnlockedEvent(string skillId)
    {
        this.skillId = skillId;
    }
}