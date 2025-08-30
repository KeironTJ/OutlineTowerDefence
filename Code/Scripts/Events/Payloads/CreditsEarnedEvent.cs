using UnityEngine;

[System.Serializable]
public struct CreditsEarnedEvent
{
    public float basic;
    public float premium;
    public float luxury;
    public float special;

    public CreditsEarnedEvent(float basic, float premium, float luxury, float special)
    {
        this.basic = basic;
        this.premium = premium;
        this.luxury = luxury;
        this.special = special;
    }
}