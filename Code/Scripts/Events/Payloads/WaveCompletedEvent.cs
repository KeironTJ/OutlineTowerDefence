using System;

[Serializable]
public class WaveCompletedEvent
{
    public int waveNumber;
    public int difficulty;

    public WaveCompletedEvent(int waveNumber, int difficulty)
    {
        this.waveNumber = waveNumber;
        this.difficulty = difficulty;
    }
}
