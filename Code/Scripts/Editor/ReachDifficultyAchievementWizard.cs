#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ReachDifficultyAchievementWizard : ScriptableWizard
{
    [Header("Achievement Basics")]
    public string achievementId = "ACH_DIFFICULTY_MASTER";
    public string displayName = "Difficulty Master";
    [TextArea]
    public string description = "Reward players for pushing into higher difficulty levels.";
    public AchievementCategory category = AchievementCategory.Progression;
    public Sprite icon;
    public string outputFolder = "Assets/Resources/Data/Achievements";

    [Header("Difficulty Settings")]
    public DifficultyProgression difficultyProgression;
    [Min(1)] public int minimumDifficulty = 2;
    [Min(0)] public int maximumDifficulty = 0; // 0 = auto (use progression max)

    [Header("Automatic Rewards")]
    public bool generateCurrencyRewards = true;
    public int baseFragmentsReward = 100;
    public int fragmentsPerTier = 50;
    public int basePrismsReward = 1;
    public int prismsPerTier = 1;

    [MenuItem("Tools/Achievements/Reach Difficulty Wizard")]
    private static void ShowWizard()
    {
        DisplayWizard<ReachDifficultyAchievementWizard>("Reach Difficulty Achievement", "Create / Update");
    }

    private void OnEnable()
    {
        if (difficultyProgression == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:DifficultyProgression");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                difficultyProgression = AssetDatabase.LoadAssetAtPath<DifficultyProgression>(path);
            }
        }

        if (string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(achievementId))
            displayName = achievementId;
    }

    protected override bool DrawWizardGUI()
    {
        EditorGUILayout.HelpBox("Generates or updates a ReachDifficulty achievement with one tier per difficulty level.", MessageType.Info);
        return base.DrawWizardGUI();
    }

    private void OnWizardCreate()
    {
        if (string.IsNullOrWhiteSpace(achievementId))
        {
            EditorUtility.DisplayDialog("Validation Error", "Achievement ID is required.", "OK");
            return;
        }

        if (difficultyProgression == null)
        {
            EditorUtility.DisplayDialog("Validation Error", "Assign a DifficultyProgression asset.", "OK");
            return;
        }

        int minLevel = Mathf.Max(minimumDifficulty, difficultyProgression.MinLevel);
        int maxLevel = maximumDifficulty <= 0
            ? difficultyProgression.MaxLevel
            : Mathf.Min(maximumDifficulty, difficultyProgression.MaxLevel);

        if (maxLevel < minLevel)
        {
            EditorUtility.DisplayDialog("Validation Error", "Maximum difficulty must be greater than or equal to minimum difficulty.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFolder) || !outputFolder.StartsWith("Assets"))
        {
            EditorUtility.DisplayDialog("Validation Error", "Output folder must be within the project's Assets directory.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
            AssetDatabase.Refresh();
        }

        string assetPath = Path.Combine(outputFolder, $"{achievementId}.asset");
        assetPath = assetPath.Replace("\\", "/");

        AchievementDefinition achievement = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(assetPath);
        bool isNewAsset = achievement == null;
        if (isNewAsset)
        {
            achievement = ScriptableObject.CreateInstance<AchievementDefinition>();
        }

        achievement.id = achievementId;
        achievement.displayName = string.IsNullOrWhiteSpace(displayName) ? achievementId : displayName;
        achievement.description = description;
        achievement.category = category;
        achievement.type = AchievementType.ReachDifficulty;
        achievement.icon = icon;

        var tiers = new List<AchievementTier>();
        int tierIndex = 0;
        for (int level = minLevel; level <= maxLevel; level++, tierIndex++)
        {
            var tier = new AchievementTier
            {
                tierLevel = tierIndex,
                targetAmount = level,
                tierName = $"Difficulty {level}",
                tierDescription = $"Reach difficulty level {level}.",
                rewards = GenerateRewards(tierIndex)
            };
            tiers.Add(tier);
        }

        achievement.tiers = tiers.ToArray();

        if (isNewAsset)
        {
            AssetDatabase.CreateAsset(achievement, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(achievement);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Reach Difficulty Achievement", isNewAsset ? "Achievement created successfully." : "Achievement updated successfully.", "OK");
        Selection.activeObject = achievement;
    }

    private AchievementReward[] GenerateRewards(int tierIndex)
    {
        if (!generateCurrencyRewards)
            return Array.Empty<AchievementReward>();

        var rewards = new List<AchievementReward>();

        int fragmentReward = baseFragmentsReward + fragmentsPerTier * tierIndex;
        if (fragmentReward > 0)
        {
            rewards.Add(new AchievementReward
            {
                rewardType = AchievementRewardType.Currency,
                currencyType = CurrencyType.Fragments,
                amount = fragmentReward
            });
        }

        int prismReward = basePrismsReward + prismsPerTier * tierIndex;
        if (prismReward > 0)
        {
            rewards.Add(new AchievementReward
            {
                rewardType = AchievementRewardType.Currency,
                currencyType = CurrencyType.Prisms,
                amount = prismReward
            });
        }

        return rewards.Count > 0 ? rewards.ToArray() : Array.Empty<AchievementReward>();
    }
}
#endif
