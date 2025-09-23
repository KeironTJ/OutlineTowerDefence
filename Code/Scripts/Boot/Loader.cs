using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Loader : MonoBehaviour
{
    [SerializeField] string mainSceneName = "MainMenu";
    [SerializeField] float minShowSeconds = 3f;
    [SerializeField] UnityEngine.UI.Slider progressBar;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] float cloudWaitTimeoutSeconds = 8f;

    private IEnumerator Start()
    {
        var startTime = Time.unscaledTime;

        // Wait local load
        while (SaveManager.main == null || !SaveManager.main.InitialLoadComplete)
            yield return null;

        // Wait for CloudSyncService to exist
        while (CloudSyncService.main == null)
            yield return null;

        var cloud = CloudSyncService.main;

        // Wait for cloud adoption to actually complete (prefer explicit completed flag / Task),
        // but don't block forever — use timeout.
        float waitStart = Time.unscaledTime;
        while (!cloud.InitialAdoptCompleted && (cloud.SyncCompleted == null || !cloud.SyncCompleted.IsCompleted) && Time.unscaledTime - waitStart < cloudWaitTimeoutSeconds)
            yield return null;

        if (!cloud.InitialAdoptCompleted && (cloud.SyncCompleted == null || !cloud.SyncCompleted.IsCompleted))
            UnityEngine.Debug.LogWarning("[Loader] Cloud adoption did not complete in time; continuing with current payload.");

        // Optional tiny delay to keep splash visible
        while (Time.unscaledTime - startTime < minShowSeconds)
            yield return null;

        // Begin async scene load
        var op = SceneManager.LoadSceneAsync(mainSceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = p;
            if (statusText) statusText.text = "Loading " + Mathf.RoundToInt(p * 100f) + "%";
            if (op.progress >= 0.9f)
                op.allowSceneActivation = true;
            yield return null;
        }
    }
}