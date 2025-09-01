using UnityEngine;

[System.Serializable]
public struct CurrencyEarnedEvent
{
    public float fragments;
    public float cores;
    public float prisms;
    public float loops;

    public CurrencyEarnedEvent(float fragments, float cores, float prisms, float loops)
    {
        this.fragments = fragments;
        this.cores = cores;
        this.prisms = prisms;
        this.loops = loops;
    }
}