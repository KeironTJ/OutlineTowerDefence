using System;

[Serializable]
public struct SkillModifier
{
    public string token;          // assign or auto
    public float additive;        // +flat
    public float multiplicative;  // *mult (1 = neutral)

    public SkillModifier(float additive, float multiplicative = 1f, string token = null)
    {
        this.additive = additive;
        this.multiplicative = multiplicative;
        this.token = token;
    }
}