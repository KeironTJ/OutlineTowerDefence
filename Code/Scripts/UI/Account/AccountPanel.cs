using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;
using System.Threading.Tasks;

public class AccountPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button linkGoogleButton;
    [SerializeField] private Button forceUploadButton;
    [SerializeField] private Button forceDownloadButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button signOutButton; // debug only

    [Header("Options")]
    [SerializeField] private bool autoHideIfLinked = false;
    [SerializeField] private bool simulateGoogle = true;   // development stub

    private void Awake()
    {
        if (linkGoogleButton) linkGoogleButton.onClick.AddListener(OnLinkGoogleClicked);
        if (forceUploadButton) forceUploadButton.onClick.AddListener(() => CloudSyncService.main?.ForceUploadNow());
        if (forceDownloadButton) forceDownloadButton.onClick.AddListener(() => CloudSyncService.main?.ForceDownloadNow());
        if (refreshButton) refreshButton.onClick.AddListener(Refresh);
        if (signOutButton) signOutButton.onClick.AddListener(OnSignOutClicked);
    }

    private void OnEnable() => Refresh();

    private void Refresh()
    {
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            statusText.text = $"Signed in: {AuthenticationService.Instance.PlayerId}";
        else
            statusText.text = "Not signed in (editor placeholder)";

        if (autoHideIfLinked && AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
            gameObject.SetActive(false);
    }

    // Called from UI
    private async void OnLinkGoogleClicked()
    {
        // ensure method stays asynchronous (prevents CS1998 in editor/dev stub)
        await Task.Yield();

        if (simulateGoogle)
        {
            // simulate a successful link (editor/dev)
            statusText.text = "Google Link: simulated (dev)";
            return;
        }

        // real platform path (guarded when added)
        statusText.text = "Google linking not implemented in editor build.";
    }

    private void OnSignOutClicked()
    {
        if (AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            statusText.text = "Signed out (editor)";
        }
    }
}
