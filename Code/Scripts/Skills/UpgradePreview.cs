using UnityEngine;

public readonly struct UpgradePreview
{
    public readonly string id;
    public readonly int fromLevel;
    public readonly int toLevel;
    public readonly float currentValue;
    public readonly float nextValue;
    public readonly float cost;
    public readonly CurrencyType currency;
    public readonly bool isMaxed;

    public static UpgradePreview Maxed => new UpgradePreview
        ("", 0, 0, 0, 0, 0, CurrencyType.Fragments, true);

    public UpgradePreview(string id, int from, int to, float current, float next, float cost, CurrencyType cur, bool maxed = false)
    {
        this.id = id;
        fromLevel = from;
        toLevel = to;
        currentValue = current;
        nextValue = next;
        this.cost = cost;
        currency = cur;
        isMaxed = maxed;
    }
}