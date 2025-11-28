using System;

[Serializable]
public struct SpendCurrencyEvent
{
    public float fragments;
    public float cores;
    public float prisms;
    public float loops;
    public string source;

    public SpendCurrencyEvent(float fragments, float cores, float prisms, float loops, string source = "Unknown")
    {
        this.fragments = fragments;
        this.cores = cores;
        this.prisms = prisms;
        this.loops = loops;
        this.source = source;
    }
}