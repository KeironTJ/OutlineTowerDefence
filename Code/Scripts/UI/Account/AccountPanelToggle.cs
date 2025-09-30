using UnityEngine;

public class AccountPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && panel)
            panel.SetActive(!panel.activeSelf);
    }
}