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
        AddStatRow("Cores Earned", NumberManager.FormatLargeNumber(playerData.totalCoresEarned));
        AddStatRow("Prisms Earned", NumberManager.FormatLargeNumber(playerData.totalPrismsEarned));
        AddStatRow("Loops Earned", NumberManager.FormatLargeNumber(playerData.totalLoopsEarned));

        // Rounds Section:
        
        AddHeaderRow("Round Stats");

        int totalRoundsCompleted = playerData.totalRoundsCompleted;
        AddStatRow("Total Rounds Completed", NumberManager.FormatLargeNumber(totalRoundsCompleted, true));

        int totalWavesComplete = playerData.totalWavesCompleted;
        AddStatRow("Total Waves Completed", NumberManager.FormatLargeNumber(totalWavesComplete, true));

        // Difficulty section
        AddHeaderRow("Difficulty","",true);
        for (int i = 0; i < playerData.difficultyMaxWaveAchieved.Length; i++)
        {
            AddStatRow($"Level {i}", playerData.difficultyMaxWaveAchieved[i].ToString());
        }

        // Enemy rows
        int totalEnemiesKilled = playerData.EnemiesDestroyed.Sum(e => e.Count);

        AddHeaderRow("Enemies Destroyed", NumberManager.FormatLargeNumber(totalEnemiesKilled, true));
        var groupedEnemies = playerData.EnemiesDestroyed.GroupBy(e => e.EnemyType);
        foreach (var group in groupedEnemies)
        {
            // Type subheader
            int typeTotal = group.Sum(e => e.Count);
            AddHeaderRow(group.Key.ToString(), NumberManager.FormatLargeNumber(typeTotal, true), true);

            // Subtype rows
            foreach (var enemyData in group)
            {
                AddStatRow(enemyData.EnemySubtype.ToString(), NumberManager.FormatLargeNumber(enemyData.Count, true));
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

    private void OnEnable()
    {
        PopulateStats();
    }
}
