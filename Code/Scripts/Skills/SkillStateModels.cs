[System.Serializable]
public class PersistentSkillState
{
    public string id;
    public int baseLevel;
    public int researchLevel;
    public bool unlocked;
}

// REWORK: separate persistent base from per‑round incremental levels
public class RoundSkillState
{
    public string id;
    public int baseLevel;          // copied from persistent at round start
    public int roundLevels;        // upgrades bought this round (starts 0)
    public bool unlocked;
    public float additiveBonus;
    public float multiplicativeBonus = 1f;
}