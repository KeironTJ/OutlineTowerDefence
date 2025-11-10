using System;

/// <summary>
/// Tracks the progress of a single research item
/// </summary>
[Serializable]
public class ResearchProgressData
{
    public string researchId;
    public int currentLevel;
    public bool isResearching;
    public string startTimeIsoUtc;
    public float durationSeconds;
    
    public ResearchProgressData()
    {
        researchId = "";
        currentLevel = 0;
        isResearching = false;
        startTimeIsoUtc = "";
        durationSeconds = 0f;
    }
    
    public ResearchProgressData(string id)
    {
        researchId = id;
        currentLevel = 0;
        isResearching = false;
        startTimeIsoUtc = "";
        durationSeconds = 0f;
    }
}

/// <summary>
/// System-wide research configuration
/// </summary>
[Serializable]
public class ResearchSystemConfig
{
    public int maxConcurrentResearch = 1;
    
    public ResearchSystemConfig()
    {
        maxConcurrentResearch = 1;
    }
}
