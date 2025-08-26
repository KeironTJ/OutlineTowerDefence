using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundLoader : MonoBehaviour
{
    [Header("Loading Screen")]
    [SerializeField] private GameObject loadingScreen;

    private void Start()
    {
        StartCoroutine(InitializeManagers());
    }

    private IEnumerator InitializeManagers()
    {
        // Show the loading screen
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Ensure UIManager is initialized
        UIManager uiManager = UnityEngine.Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.Log("Initializing UIManager...");
            Instantiate(Resources.Load("UIManager")); // Ensure a prefab named "UIManager" exists in Resources
            yield return null; // Wait for one frame
        }

        // Ensure RoundManager is initialized
        RoundManager roundManager = UnityEngine.Object.FindFirstObjectByType<RoundManager>();
        if (roundManager == null)
        {
            Debug.Log("Initializing RoundManager...");
            Instantiate(Resources.Load("RoundManager")); // Ensure a prefab named "RoundManager" exists in Resources
            yield return null;
        }

        // Ensure PlayerManager is initialized
        PlayerManager playerManager = PlayerManager.main;

        // Load the main game scene
        Debug.Log("All managers initialized. Loading game scene...");
        SceneManager.LoadScene("PlayGame"); // Replace "MainGame" with the name of your game scene
    }
}
