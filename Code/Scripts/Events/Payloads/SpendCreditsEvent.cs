using System;

[Serializable]
public struct SpendCreditsEvent
{
    public float basic;
    public float premium;
    public float luxury;
    public float special;
    public string source;

    public SpendCreditsEvent(float basic, float premium, float luxury, float special, string source = "Unknown")
    {
        this.basic = basic;
        this.premium = premium;
        this.luxury = luxury;
        this.special = special;
        this.source = source;
    }
}