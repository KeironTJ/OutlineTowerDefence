using System;

[Serializable]
public class DifficultyAchievedEvent
{
    public int difficultyLevel;
    public int previousMaxDifficulty;
    public int triggeredDifficulty;
    public int waveNumber;
    public int highestWaveAtNewDifficulty;

    public DifficultyAchievedEvent()
    {
    }

    public DifficultyAchievedEvent(
        int difficultyLevel,
        int previousMaxDifficulty,
        int triggeredDifficulty,
        int waveNumber,
        int highestWaveAtNewDifficulty)
    {
        this.difficultyLevel = difficultyLevel;
        this.previousMaxDifficulty = previousMaxDifficulty;
        this.triggeredDifficulty = triggeredDifficulty;
        this.waveNumber = waveNumber;
        this.highestWaveAtNewDifficulty = highestWaveAtNewDifficulty;
    }
}
