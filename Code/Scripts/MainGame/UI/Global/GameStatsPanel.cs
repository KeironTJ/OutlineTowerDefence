using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Add this using directive for LINQ methods

public class GameStatsPanel : MonoBehaviour
{
    [Header("References")]
    public Transform contentParent; // Assign the Content object of your ScrollView
    public GameObject statItemPrefab; // Assign your StatItem prefab

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PopulateStats()
    {
        // Clear existing items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Fetch stats from PlayerData
        var playerData = PlayerManager.main.playerData;
        var stats = new List<string>
        {
            $"CURRENCY: ",
            $"Basic Credits Earned: {playerData.totalBasicCreditsEarned}",
            $"Premium Credits Earned: {playerData.totalPremiumCreditsEarned}",
            $"Special Credits Earned: {playerData.totalSpecialCreditsEarned}",
            $"Luxury Credits Earned: {playerData.totalLuxuryCreditsEarned}",
            $" "
        };

        // Add difficulty stats dynamically
        stats.Add("DIFFICULTY:");
        for (int i = 0; i < playerData.difficultyMaxWaveAchieved.Length; i++)
        {
            stats.Add($"Difficulty Level {i}: {playerData.difficultyMaxWaveAchieved[i]}");
        }
        stats.Add(" ");

        stats.Add("ENEMIES: ");

        // Group enemy destruction stats by EnemyType
        var groupedEnemies = playerData.EnemiesDestroyed.GroupBy(e => e.EnemyType);
        foreach (var group in groupedEnemies)
        {
            stats.Add($"{group.Key}:");
            foreach (var enemyData in group)
            {
                stats.Add($"  {enemyData.EnemySubtype}: {enemyData.Count}");
            }
        }

        stats.Add($" ");

        // Add new items
        foreach (string stat in stats)
        {
            GameObject statItem = Instantiate(statItemPrefab, contentParent);
            // Locate the TMP_Text component on the child named "StatItemText"
            var textComponent = statItem.transform.Find("StatItemText").GetComponent<TMPro.TMP_Text>();
            textComponent.text = stat;
        }
    }

    private void OnEnable()
    {
        // Populate stats when the panel is enabled
        PopulateStats();
    }
}
