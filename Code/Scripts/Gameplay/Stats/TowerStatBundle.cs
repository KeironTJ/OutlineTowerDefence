using System;

public readonly struct TowerStatBundle
{
    private static readonly float[] EmptyValues = new float[(int)StatId.Count];
    private readonly float[] values;

    public TowerStatBundle(float[] values)
    {
        this.values = values ?? EmptyValues;
    }

    public float this[StatId stat] => values[(int)stat];

    // Attack
    public float AttackDamage => this[StatId.AttackDamage];
    public float AttackSpeed => this[StatId.AttackSpeed];
    public float TargetingRange => this[StatId.TargetingRange];
    public float BulletSpeed => this[StatId.BulletSpeed];
    public float CritChance => this[StatId.CritChance];
    public float CritMultiplier => this[StatId.CritMultiplier];

    // Defence
    public float MaxHealth => this[StatId.MaxHealth];
    public float HealPerSecond => this[StatId.HealPerSecond];
    public float ArmorPercent => this[StatId.ArmorPercent];

    // Utility
    public float FragmentMultiplier => this[StatId.FragmentMultiplier];

    public float GetOrDefault(StatId stat, float fallback)
    {
        float value = this[stat];
        if (float.IsNaN(value) || float.IsInfinity(value))
            return fallback;
        return Math.Abs(value) < float.Epsilon ? fallback : value;
    }

    public static TowerStatBundle Empty => new TowerStatBundle(EmptyValues);
}
