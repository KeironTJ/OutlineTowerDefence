using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionsUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject profilePanel;
    public GameObject gameStatsPanel;
    public GameObject settingsPanel;

    private static OptionsUIManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Make this object persistent across scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicate instances
        }
    }

    public static OptionsUIManager Instance
    {
        get { return instance; }
    }

    public void ShowPanel(GameObject panel)
    {
        // Ensure only the specified panel is visible
        HideAllPanels();
        panel.SetActive(true);
    }

    public void HideAllPanels()
    {
        // Hide all panels managed by this UI
        optionsPanel.SetActive(false);
        profilePanel.SetActive(false);
        gameStatsPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void TogglePanel(GameObject panel)
    {
        // Toggle the visibility of the specified panel
        panel.SetActive(!panel.activeSelf);
    }

    public void ToggleOptionsUIManager()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void EnableOptionsUIManager()
    {
        gameObject.SetActive(true);
    }

    public void DisableOptionsUIManager()
    {
        gameObject.SetActive(false);
    }
}
