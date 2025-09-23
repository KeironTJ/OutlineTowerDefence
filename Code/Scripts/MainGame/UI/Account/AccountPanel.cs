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

    // Development-friendly stub: no native Google Play calls here.
    private async void OnLinkGoogleClicked()
    {
        if (simulateGoogle)
        {
            // simulate a successful link (editor/dev)
            statusText.text = "Google Link: simulated (dev)";
            // Optionally trigger a cloud sync sign-in flow or set flags here
            return;
        }

        // If you later add real Android bridge, implement guarded call here:
        // #if UNITY_ANDROID
        //    var (ok, code) = await GooglePlayAuthBridge.SignInAndGetServerAuthCode();
        //    if (ok) { /* handle code */ }
        // #endif

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
