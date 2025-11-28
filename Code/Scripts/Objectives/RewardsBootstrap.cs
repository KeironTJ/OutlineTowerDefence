using UnityEngine;
using System.Collections;

public class RewardsBootstrap : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(InitWhenSaveReady());
    }

    private IEnumerator InitWhenSaveReady()
    {
        while (SaveManager.main == null || SaveManager.main.Current?.player == null)
            yield return null;

        DailyObjectiveManager.main?.EnsureInitialized();
        WeeklyObjectiveManager.main?.EnsureInitialized();
    }
}