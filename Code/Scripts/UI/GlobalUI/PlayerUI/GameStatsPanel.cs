using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class GameStatsPanel : MonoBehaviour
{
    [Header("References")]
    public Transform contentParent;
    public GameObject statItemPrefab;

    public void PopulateStats()
    {
        // Clear existing items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        var playerData = PlayerManager.main.playerData;

        // Currency section
        AddHeaderRow("Currency");
        AddStatRow("Fragments Earned", NumberManager.FormatLargeNumber(playerData.totalFragmentsEarned));
        AddStatRow("Cores Earned",      NumberManager.FormatLargeNumber(playerData.totalCoresEarned));
        AddStatRow("Prisms Earned",     NumberManager.FormatLargeNumber(playerData.totalPrismsEarned));
        AddStatRow("Loops Earned",      NumberManager.FormatLargeNumber(playerData.totalLoopsEarned));

        // Rounds Section:
        AddHeaderRow("Round Stats");

        int totalRoundsCompleted = playerData.totalRoundsCompleted;
        AddStatRow("Total Rounds Completed", NumberManager.FormatLargeNumber(totalRoundsCompleted, true));

        int totalWavesComplete = playerData.totalWavesCompleted;
        AddStatRow("Total Waves Completed", NumberManager.FormatLargeNumber(totalWavesComplete, true));
        
        

        // Difficulty section
        AddHeaderRow("Difficulty","Best Wave",true);
        var difficultyArray = playerData.difficultyMaxWaveAchieved;
        if (difficultyArray != null && difficultyArray.Length > 0)
        {
            var pm = PlayerManager.main;
            int minLevel = pm ? pm.GetMinDifficultyLevel() : 1;
            for (int index = 0; index < difficultyArray.Length; index++)
            {
                int level = minLevel + index;
                AddStatRow($"Level {level}", NumberManager.FormatLargeNumber(difficultyArray[index], true));
            }
        }

        // Combat Stats
        AddHeaderRow("Combat Stats");
        AddStatRow("Projectiles Shot", NumberManager.FormatLargeNumber(playerData.lifetimeShotsFired, true));
        AddStatRow("Total Damage Dealt", NumberManager.FormatLargeNumber(playerData.lifetimeTotalDamage));
        AddStatRow("Critical Hits", NumberManager.FormatLargeNumber(playerData.lifetimeCriticalHits, true));
        
        
        // Turret Stats
        if (playerData.lifetimeTurretStats != null && playerData.lifetimeTurretStats.Count > 0)
        {
            AddHeaderRow("Turrets", "", true);
            foreach (var turret in playerData.lifetimeTurretStats.OrderByDescending(t => t.shotsFired).Take(10))
            {
                string turretName = DefinitionDisplayNameUtility.GetTurretName(turret.turretId);
                AddStatRow($"{turretName} Shots", NumberManager.FormatLargeNumber(turret.shotsFired, true));
            }
        }


        // Projectile Stats
        if (playerData.lifetimeProjectileStats != null && playerData.lifetimeProjectileStats.Count > 0)
        {
            AddHeaderRow("Projectiles", "", true);
            foreach (var proj in playerData.lifetimeProjectileStats.OrderByDescending(p => p.shotsFired).Take(10))
            {
                string projName = DefinitionDisplayNameUtility.GetProjectileName(proj.projectileId);
                AddStatRow($"{projName} Shots", NumberManager.FormatLargeNumber(proj.shotsFired, true));
                AddStatRow($"{projName} Damage", NumberManager.FormatLargeNumber(proj.damageDealt));
            }
        }

        

        // Enemy kills
        var kills = playerData.enemyKills ?? new List<EnemyKillEntry>();
        int totalEnemiesKilled = kills.Sum(k => k.count);
        AddHeaderRow("Enemies Destroyed", NumberManager.FormatLargeNumber(totalEnemiesKilled, true));

        if (totalEnemiesKilled == 0)
        {
            AddStatRow("No kills recorded", "-");
        }
        else
        {
            // Group by tier, then list each definition (ordered by count desc)
            foreach (var tierGroup in kills
                     .GroupBy(k => k.tier)
                     .OrderBy(g => g.Key)) // basic -> boss (enum order)
            {
                int tierTotal = tierGroup.Sum(k => k.count);
                AddHeaderRow(tierGroup.Key.ToString(), NumberManager.FormatLargeNumber(tierTotal, true), true);

                foreach (var entry in tierGroup
                             .OrderByDescending(e => e.count)
                             .ThenBy(e => e.definitionId))
                {
                    string enemyName = DefinitionDisplayNameUtility.GetEnemyName(entry.definitionId);
                    AddStatRow(enemyName,
                        NumberManager.FormatLargeNumber(entry.count, true));
                }
            }
        }
    }

    private void AddStatRow(string label, string value)
    {
        GameObject statItem = Instantiate(statItemPrefab, contentParent);
        var labelText = statItem.transform.Find("LabelText").GetComponent<TMPro.TMP_Text>();
        var valueText = statItem.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        labelText.text = label;
        valueText.text = value;
    }

    private void AddHeaderRow(string label, string value = "", bool isSubHeader = false)
    {
        GameObject statItem = Instantiate(statItemPrefab, contentParent);
        var labelText = statItem.transform.Find("LabelText").GetComponent<TMPro.TMP_Text>();
        var valueText = statItem.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        labelText.text = label;
        valueText.text = value;

        if (!isSubHeader)
        {
            labelText.fontStyle = TMPro.FontStyles.Bold;
            labelText.fontStyle |= TMPro.FontStyles.Underline;
            labelText.fontSize *= 1.2f;

            valueText.fontStyle = TMPro.FontStyles.Bold;
            valueText.fontStyle |= TMPro.FontStyles.Underline;
            valueText.fontSize *= 1.2f;
        }

        else if (isSubHeader)
        {
            labelText.fontStyle = TMPro.FontStyles.Bold;
            labelText.fontSize *= 1.1f;

            valueText.fontStyle = TMPro.FontStyles.Bold;
            valueText.fontSize *= 1.1f;
        }
    }

    private void PopulateEnemyKills(PlayerData data)
    {
        // (Deprecated helper – now integrated directly in PopulateStats)
    }

    private void AddEnemyKillLine(string text)
    {
        // Use a single-column stat row (label only)
        GameObject statItem = Instantiate(statItemPrefab, contentParent);
        var labelText = statItem.transform.Find("LabelText").GetComponent<TMPro.TMP_Text>();
        var valueText = statItem.transform.Find("ValueText").GetComponent<TMPro.TMP_Text>();
        labelText.text = text;
        valueText.text = "";
    }

    private void OnEnable()
    {
        PopulateStats();
    }
}
