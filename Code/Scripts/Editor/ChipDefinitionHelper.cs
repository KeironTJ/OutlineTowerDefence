using UnityEngine;
using UnityEditor;
using System.IO;

public class ChipDefinitionHelper : EditorWindow
{
    [MenuItem("Outline/Create Example Chips")]
    public static void CreateExampleChips()
    {
        string folderPath = "Assets/Resources/Data/Chips";
        
        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Resources/Data", "Chips");
        
        // Example 1: Attack Damage Boost
        CreateChip(
            folderPath,
            "ATK_BOOST_01",
            "Power Surge",
            "Increases attack damage by a percentage",
            ChipBonusType.AttackDamageMultiplier,
            5.0f,
            3.0f,
            "+{0}%"
        );
        
        // Example 2: Attack Speed Boost
        CreateChip(
            folderPath,
            "ATK_SPD_01",
            "Rapid Fire",
            "Increases attack speed for faster shots",
            ChipBonusType.AttackSpeed,
            8.0f,
            4.0f,
            "+{0}%"
        );
        
        // Example 3: Health Boost
        CreateChip(
            folderPath,
            "HEALTH_01",
            "Fortify",
            "Increases tower maximum health",
            ChipBonusType.Health,
            10.0f,
            5.0f,
            "+{0}%"
        );
        
        // Example 4: Fragments Boost
        CreateChip(
            folderPath,
            "FRAG_BOOST_01",
            "Wealth",
            "Increases fragments earned from enemies",
            ChipBonusType.FragmentsBoost,
            15.0f,
            5.0f,
            "+{0}%"
        );
        
        // Example 5: Critical Chance
        CreateChip(
            folderPath,
            "CRIT_CHANCE_01",
            "Lucky Shot",
            "Increases chance for critical hits",
            ChipBonusType.CriticalChance,
            5.0f,
            2.0f,
            "+{0}%"
        );
        
        // Example 6: Critical Damage
        CreateChip(
            folderPath,
            "CRIT_DMG_01",
            "Devastation",
            "Increases damage of critical hits",
            ChipBonusType.CriticalDamage,
            25.0f,
            10.0f,
            "+{0}%"
        );
        
        // Example 7: Health Recovery
        CreateChip(
            folderPath,
            "HEALTH_REGEN_01",
            "Regeneration",
            "Increases health recovery speed",
            ChipBonusType.HealthRecoverySpeed,
            10.0f,
            5.0f,
            "+{0}%"
        );
        
        // Example 8: Projectile Speed
        CreateChip(
            folderPath,
            "PROJ_SPD_01",
            "Velocity",
            "Increases projectile travel speed",
            ChipBonusType.ProjectileSpeed,
            10.0f,
            5.0f,
            "+{0}%"
        );
        
        // Example 9: Turret Range
        CreateChip(
            folderPath,
            "RANGE_01",
            "Eagle Eye",
            "Increases turret targeting range",
            ChipBonusType.TurretRange,
            8.0f,
            4.0f,
            "+{0}%"
        );
        
        // Example 10: Experience Boost
        CreateChip(
            folderPath,
            "EXP_BOOST_01",
            "Knowledge",
            "Increases experience gained",
            ChipBonusType.ExperienceBoost,
            12.0f,
            6.0f,
            "+{0}%"
        );
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[ChipDefinitionHelper] Created 10 example chips in {folderPath}");
        EditorUtility.DisplayDialog(
            "Chips Created",
            $"Successfully created 10 example chip definitions in:\n{folderPath}\n\nDon't forget to add them to ChipService's loadedDefinitions array!",
            "OK"
        );
    }
    
    private static void CreateChip(
        string folderPath,
        string id,
        string chipName,
        string description,
        ChipBonusType bonusType,
        float baseBonus,
        float bonusPerRarity,
        string bonusFormat)
    {
        string assetPath = $"{folderPath}/{id}.asset";
        
        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<ChipDefinition>(assetPath);
        if (existing != null)
        {
            Debug.LogWarning($"[ChipDefinitionHelper] Chip {id} already exists, skipping");
            return;
        }
        
        var chip = ScriptableObject.CreateInstance<ChipDefinition>();
        chip.id = id;
        chip.chipName = chipName;
        chip.description = description;
        chip.bonusType = bonusType;
        chip.baseBonus = baseBonus;
        chip.bonusPerRarity = bonusPerRarity;
        chip.bonusFormat = bonusFormat;
        chip.chipsNeededForRarity = new int[] { 0, 3, 5, 7, 10 };
        chip.canChangeInRound = true;
        chip.unlockWave = 1;
        
        AssetDatabase.CreateAsset(chip, assetPath);
        Debug.Log($"[ChipDefinitionHelper] Created chip: {chipName} at {assetPath}");
    }
}
