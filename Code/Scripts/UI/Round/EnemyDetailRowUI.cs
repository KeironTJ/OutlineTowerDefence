using System;
using TMPro;
using UnityEngine;

public class EnemyDetailRowUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI traitsText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI dpsText;
    [SerializeField] private TextMeshProUGUI rewardsText;

    public void BindHeader(
        string nameLabel = "Enemy",
        string traitsLabel = "Traits",
        string healthLabel = "Health",
        string speedLabel = "Speed",
        string dpsLabel = "DPS",
        string rewardsLabel = "Rewards")        
    {
        if (nameText) nameText.text = nameLabel;
        if (traitsText) traitsText.text = traitsLabel;
        if (healthText) healthText.text = healthLabel;
        if (speedText) speedText.text = speedLabel;
        if (dpsText) dpsText.text = dpsLabel;
        if (rewardsText) rewardsText.text = rewardsLabel;
    }

    public void Bind(EnemyTypeDefinition definition, EnemyDetailPanelUI.EnemyPreviewStats stats, int killCount)
    {
        if (!definition)
        {
            gameObject.SetActive(false);
            return;
        }

        if (nameText)
            nameText.text = string.IsNullOrEmpty(definition.displayName) ? definition.name : definition.displayName;

        if (traitsText)
            traitsText.text = FormatTraits(definition.traits);

        if (healthText)
            healthText.text = NumberManager.FormatLargeNumber(stats.health);

        if (speedText)
            speedText.text = NumberManager.FormatLargeNumber(stats.speed);

        if (dpsText)
            dpsText.text = NumberManager.FormatLargeNumber(stats.DamagePerSecond);

        if (rewardsText)
            rewardsText.text = FormatRewards(stats.fragments, stats.cores, stats.prisms, stats.loops);

    }

    private static string FormatTraits(EnemyTrait traits)
    {
        if (traits == EnemyTrait.None)
            return "No Trait";

        var names = Enum.GetValues(typeof(EnemyTrait));
        var parts = Array.Empty<string>();

        int count = 0;
        foreach (EnemyTrait flag in names)
        {
            if (flag == EnemyTrait.None) continue;
            if ((traits & flag) == flag)
                count++;
        }

        if (count == 0)
            return "No Trait";

        parts = new string[count];
        int idx = 0;
        foreach (EnemyTrait flag in names)
        {
            if (flag == EnemyTrait.None) continue;
            if ((traits & flag) == flag)
                parts[idx++] = flag.ToString();
        }

        return string.Join(", ", parts);
    }

    private static string FormatRewards(int fragments, int cores, int prisms, int loops)
    {
        static string Part(string label, int value) => value > 0 ? $"{value} {label}" : null;

        var parts = new string[]
        {
            Part("Fragments", fragments),
            Part("Cores", cores),
            Part("Prisms", prisms),
            Part("Loops", loops)    
        };

        int nonNull = 0;
        foreach (var part in parts)
            if (!string.IsNullOrEmpty(part))
                nonNull++;

        if (nonNull == 0)
            return "No reward";

        var filtered = new string[nonNull];
        int index = 0;
        foreach (var part in parts)
            if (!string.IsNullOrEmpty(part))
                filtered[index++] = part;

        return string.Join(", ", filtered);
    }
}
