using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundSkillData
{
    public Dictionary<string, Skill> attackSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> defenceSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> supportSkills = new Dictionary<string, Skill>();
    public Dictionary<string, Skill> specialSkills = new Dictionary<string, Skill>();
}

