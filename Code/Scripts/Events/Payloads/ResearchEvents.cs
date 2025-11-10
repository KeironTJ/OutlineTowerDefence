using System;

[Serializable]
public class ResearchStartedEvent
{
    public string researchId;
    public string researchName;
    
    public ResearchStartedEvent(string id, string name)
    {
        researchId = id;
        researchName = name;
    }
}

[Serializable]
public class ResearchCompletedEvent
{
    public string researchId;
    public string researchName;
    public int level;
    
    public ResearchCompletedEvent(string id, string name, int lvl)
    {
        researchId = id;
        researchName = name;
        level = lvl;
    }
}
