using System;

[Serializable]
public class WaveCompletedEvent
{
    public int waveNumber;

    public WaveCompletedEvent(int waveNumber)
    {
        this.waveNumber = waveNumber;
    }
}
