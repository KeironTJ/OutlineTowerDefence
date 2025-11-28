using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("Auto-Pause Settings")]
    [SerializeField] private Toggle autoPauseToggle;
    [SerializeField] private TextMeshProUGUI autoPauseLabel;
    
    private TimeScaleManager timeScaleManager;
    
    void Start()
    {
        timeScaleManager = TimeScaleManager.Instance;
        
        if (autoPauseToggle != null)
        {
            // Load current setting
            if (timeScaleManager != null)
            {
                autoPauseToggle.isOn = timeScaleManager.AutoPauseOnOptions;
            }
            
            // Add listener for changes
            autoPauseToggle.onValueChanged.AddListener(OnAutoPauseToggleChanged);
        }
        
        UpdateAutoPauseLabel();
    }
    
    void OnDestroy()
    {
        if (autoPauseToggle != null)
        {
            autoPauseToggle.onValueChanged.RemoveListener(OnAutoPauseToggleChanged);
        }
    }
    
    private void OnAutoPauseToggleChanged(bool value)
    {
        if (timeScaleManager != null)
        {
            timeScaleManager.AutoPauseOnOptions = value;
            timeScaleManager.SaveAutoPauseSetting();
        }
        UpdateAutoPauseLabel();
    }
    
    private void UpdateAutoPauseLabel()
    {
        if (autoPauseLabel != null)
        {
            autoPauseLabel.text = "Auto-pause when options menu opens";
        }
    }
}
