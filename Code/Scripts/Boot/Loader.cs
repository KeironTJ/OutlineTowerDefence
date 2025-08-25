using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Loader : MonoBehaviour
{
    [SerializeField] string mainSceneName = "MainMenu";
    [SerializeField] float minShowSeconds = 0.5f;
    [SerializeField] UnityEngine.UI.Slider progressBar;
    [SerializeField] TextMeshProUGUI statusText;

    private IEnumerator Start()
    {
        var startTime = Time.unscaledTime;
        // Wait local load
        while (SaveManager.main == null || !SaveManager.main.InitialLoadComplete)
            yield return null;

        // Wait cloud adoption attempt
        while (CloudSyncService.main == null || !CloudSyncService.main.InitialAdoptAttempted)
            yield return null;

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