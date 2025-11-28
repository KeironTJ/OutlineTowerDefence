#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DifficultyWaveAchievementWizard : ScriptableWizard
{
    [Header("Achievement Basics")]
    public string achievementId = "ACH_DIFFICULTY_WAVE_MASTER";
    public string displayName = "Difficulty Wave Master";
    [TextArea]
    public string description = "Reward players for reaching deep waves at each difficulty level.";
    public AchievementCategory category = AchievementCategory.Progression;
    public Sprite icon;
    public string outputFolder = "Assets/Resources/Data/Achievements";

    [Header("Difficulty Settings")]
    [Min(1)] public int minimumDifficulty = 1;
    [Min(1)] public int maximumDifficulty = 5;
    public List<int> waveMilestones = new List<int> { 10, 20, 50, 100, 500, 1000 };

    [Header("Output Options")]
    public bool createSeparateAssetPerDifficulty = false;
    public string perDifficultyIdFormat = "{0}_D{1}";
    public string perDifficultyDisplayNameFormat = "{0} - Difficulty {1}";

    [Header("Automatic Rewards")]
    public bool generateCurrencyRewards = true;
    public int baseCoresReward = 25;
    public int coresPerTier = 25;
    public int coresPerDifficultyBonus = 50;
    public int basePrismsReward = 5;
    public int prismsPerTier = 3;
    public int prismsPerDifficultyBonus = 10;

    [MenuItem("Tools/Achievements/Difficulty Wave Wizard")]
    private static void ShowWizard()
    {
        DisplayWizard<DifficultyWaveAchievementWizard>("Difficulty Wave Achievement", "Create / Update");
    }

    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(achievementId))
            displayName = achievementId;
    }

    protected override bool DrawWizardGUI()
    {
        EditorGUILayout.HelpBox("Creates an achievement with tiers for reaching specific wave milestones at each difficulty level.", MessageType.Info);
        return base.DrawWizardGUI();
    }

    private void OnWizardCreate()
    {
        if (string.IsNullOrWhiteSpace(achievementId))
        {
            EditorUtility.DisplayDialog("Validation Error", "Achievement ID is required.", "OK");
            return;
        }

        if (waveMilestones == null || waveMilestones.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Error", "Provide at least one wave milestone.", "OK");
            return;
        }

        waveMilestones.RemoveAll(m => m <= 0);
        if (waveMilestones.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation Error", "Wave milestones must all be positive.", "OK");
            return;
        }

        if (maximumDifficulty < minimumDifficulty)
        {
            EditorUtility.DisplayDialog("Validation Error", "Maximum difficulty must be greater than or equal to minimum difficulty.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder) || !outputFolder.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("Validation Error", "Output folder must live inside the project's Assets directory.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        string assetPath = Path.Combine(outputFolder, $"{achievementId}.asset");
        assetPath = assetPath.Replace("\\", "/");

        waveMilestones.Sort();

        if (createSeparateAssetPerDifficulty)
        {
            int count = CreateAssetsPerDifficulty();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Difficulty Wave Achievement", $"Generated {count} difficulty-specific achievement {(count == 1 ? "asset" : "assets")}.", "OK");
        }
        else
        {
            var achievement = CreateSingleAchievement(assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = achievement;
            EditorUtility.DisplayDialog("Difficulty Wave Achievement", $"Achievement '{achievement.displayName}' saved to {assetPath}.", "OK");
        }
    }

    private AchievementDefinition CreateSingleAchievement(string assetPath)
    {
        var achievement = LoadOrCreate(assetPath);

        achievement.id = achievementId;
        achievement.displayName = string.IsNullOrWhiteSpace(displayName) ? achievementId : displayName;
        achievement.description = description;
        achievement.category = category;
        achievement.type = AchievementType.CompleteDifficultyWaves;
        achievement.icon = icon;

        var tiers = new List<AchievementTier>();
        int tierIndex = 0;
        for (int difficulty = minimumDifficulty; difficulty <= maximumDifficulty; difficulty++)
        {
            tiers.AddRange(BuildTiersForDifficulty(difficulty, ref tierIndex));
        }

        achievement.tiers = tiers.ToArray();

        FinalizeAsset(assetPath, achievement);
        return achievement;
    }

    private int CreateAssetsPerDifficulty()
    {
        string baseDisplay = string.IsNullOrWhiteSpace(displayName) ? achievementId : displayName;
        int processed = 0;
        AchievementDefinition lastAchievement = null;

        for (int difficulty = minimumDifficulty; difficulty <= maximumDifficulty; difficulty++)
        {
            string diffId = SafeFormat(perDifficultyIdFormat, achievementId, difficulty);
            if (string.IsNullOrWhiteSpace(diffId))
                diffId = $"{achievementId}_D{difficulty}";

            string diffDisplay = SafeFormat(perDifficultyDisplayNameFormat, baseDisplay, difficulty);
            if (string.IsNullOrWhiteSpace(diffDisplay))
                diffDisplay = $"{baseDisplay} (Difficulty {difficulty})";

            string assetPath = Path.Combine(outputFolder, $"{diffId}.asset").Replace("\\", "/");

            var achievement = LoadOrCreate(assetPath);
            achievement.id = diffId;
            achievement.displayName = diffDisplay;
            achievement.description = description;
            achievement.category = category;
            achievement.type = AchievementType.CompleteDifficultyWaves;
            achievement.icon = icon;

            int tierIndex = 0;
            var tiers = BuildTiersForDifficulty(difficulty, ref tierIndex);
            achievement.tiers = tiers.ToArray();

            FinalizeAsset(assetPath, achievement);
            processed++;
            lastAchievement = achievement;
        }

        if (lastAchievement != null)
            Selection.activeObject = lastAchievement;

        return processed;
    }

    private List<AchievementTier> BuildTiersForDifficulty(int difficulty, ref int tierIndex)
    {
        var tiers = new List<AchievementTier>(waveMilestones.Count);
        for (int m = 0; m < waveMilestones.Count; m++, tierIndex++)
        {
            int waveTarget = waveMilestones[m];
            tiers.Add(new AchievementTier
            {
                tierLevel = tierIndex,
                requiredDifficultyLevel = difficulty,
                targetAmount = waveTarget,
                tierName = $"Difficulty {difficulty} Wave {waveTarget}",
                tierDescription = $"Reach wave {waveTarget} on difficulty {difficulty}.",
                rewards = GenerateRewards(difficulty, tierIndex)
            });
        }

        return tiers;
    }

    private AchievementDefinition LoadOrCreate(string assetPath)
    {
        var achievement = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(assetPath);
        if (achievement == null)
        {
            achievement = ScriptableObject.CreateInstance<AchievementDefinition>();
        }

        return achievement;
    }

    private void FinalizeAsset(string assetPath, AchievementDefinition achievement)
    {
        if (AssetDatabase.Contains(achievement))
        {
            EditorUtility.SetDirty(achievement);
        }
        else
        {
            AssetDatabase.CreateAsset(achievement, assetPath);
        }
    }

    private AchievementReward[] GenerateRewards(int difficulty, int tierIndex)
    {
        if (!generateCurrencyRewards)
            return System.Array.Empty<AchievementReward>();

        var rewards = new List<AchievementReward>();

        int coresReward = baseCoresReward + coresPerTier * tierIndex + coresPerDifficultyBonus * (difficulty - minimumDifficulty);
        if (coresReward > 0)
        {
            rewards.Add(new AchievementReward
            {
                rewardType = AchievementRewardType.Currency,
                currencyType = CurrencyType.Cores,
                amount = coresReward
            });
        }

        int prismReward = basePrismsReward + prismsPerTier * tierIndex + prismsPerDifficultyBonus * (difficulty - minimumDifficulty);
        if (prismReward > 0)
        {
            rewards.Add(new AchievementReward
            {
                rewardType = AchievementRewardType.Currency,
                currencyType = CurrencyType.Prisms,
                amount = prismReward
            });
        }

        return rewards.Count > 0 ? rewards.ToArray() : System.Array.Empty<AchievementReward>();
    }

    private static string SafeFormat(string format, string baseValue, int difficulty)
    {
        if (string.IsNullOrWhiteSpace(format))
            return null;

        try
        {
            return string.Format(format, baseValue, difficulty);
        }
        catch
        {
            return null;
        }
    }
}
#endif
