using UnityEngine;

public sealed class StatCollector
{
    private readonly StatAccumulator[] _accumulators;

    public StatCollector()
    {
        _accumulators = new StatAccumulator[(int)StatId.Count];
        for (int i = 0; i < _accumulators.Length; i++)
            _accumulators[i] = StatAccumulator.Create();
    }

    public void AddBase(StatId stat, float value)
    {
        int index = (int)stat;
        var acc = _accumulators[index];
        acc.AddBase(value);
        _accumulators[index] = acc;
    }

    public void AddFlatBonus(StatId stat, float value)
    {
        int index = (int)stat;
        var acc = _accumulators[index];
        acc.AddFlat(value);
        _accumulators[index] = acc;
    }

    public void AddMultiplier(StatId stat, float multiplier)
    {
        int index = (int)stat;
        var acc = _accumulators[index];
        acc.Multiply(multiplier);
        _accumulators[index] = acc;
    }

    public void AddPercentage(StatId stat, float percentage)
    {
        AddMultiplier(stat, 1f + percentage);
    }

    public TowerStatBundle BuildBundle()
    {
        var values = new float[_accumulators.Length];
        for (int i = 0; i < values.Length; i++)
            values[i] = _accumulators[i].Resolve();
        return new TowerStatBundle(values);
    }

    private struct StatAccumulator
    {
        public float BaseValue;
        public float FlatBonus;
        public float Multiplier;

        public static StatAccumulator Create()
        {
            return new StatAccumulator
            {
                BaseValue = 0f,
                FlatBonus = 0f,
                Multiplier = 1f
            };
        }

        public void AddBase(float value)
        {
            BaseValue += value;
        }

        public void AddFlat(float value)
        {
            FlatBonus += value;
        }

        public void Multiply(float factor)
        {
            float safeFactor = Mathf.Approximately(factor, 0f) ? 1f : factor;
            Multiplier *= safeFactor;
        }

        public float Resolve()
        {
            return (BaseValue + FlatBonus) * Multiplier;
        }
    }
}
