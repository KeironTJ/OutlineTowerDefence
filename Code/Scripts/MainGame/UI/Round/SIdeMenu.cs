using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SIdeMenu : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Button settingsButton;

    private Tower tower;

    private bool isMenuOpen = true;


    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FindTowerInstance());
        ToggleMenu();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator FindTowerInstance()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (tower == null)
        {
            tower = FindObjectOfType<Tower>();
            if (tower == null)
            {
                yield return wait;
            }
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        anim.SetBool("MenuOpen", isMenuOpen);
    }

    public void EndRound()
    {
        if (tower != null)
        {
            tower.OnTowerDestroyed();
        }
        else
        {
            Debug.LogWarning("Tower instance is not found.");
        }
    }

    public void OpenSettings()
    {
        // Open the settings menu
        OptionsUIManager.Instance.OpenOptions();
    }
}
