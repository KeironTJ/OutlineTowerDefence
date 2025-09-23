using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;

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
    [SerializeField] private bool simulateGoogle = true;   // turn off after real setup

    private void Awake()
    {
        if (linkGoogleButton)    linkGoogleButton.onClick.AddListener(OnLinkGoogle);
        if (forceUploadButton)   forceUploadButton.onClick.AddListener(() => CloudSyncService.main?.ForceUploadNow());
        if (forceDownloadButton) forceDownloadButton.onClick.AddListener(() => CloudSyncService.main?.ForceDownloadNow());
        if (refreshButton)       refreshButton.onClick.AddListener(Refresh);
        if (signOutButton)       signOutButton.onClick.AddListener(DebugSignOut);
    }

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        if (GameServicesInitializer.main == null)
        {
            SetStatus("Auth system not present.");
            SetInteractable(false);
            return;
        }

        if (!GameServicesInitializer.main.SignedIn)
        {
            SetStatus("Signing in...");
            SetInteractable(false);
            return;
        }

        var playerId = AuthenticationService.Instance.PlayerId;
        bool googleLinked = GameServicesInitializer.main.HasProvider(GameServicesInitializer.ProviderGooglePlayGames);

        SetStatus(googleLinked
            ? $"Linked: Google\nPlayerId:\n{playerId}"
            : $"Guest (Not Linked)\nPlayerId:\n{playerId}");

        if (linkGoogleButton) linkGoogleButton.interactable = !googleLinked;
        if (autoHideIfLinked && googleLinked) gameObject.SetActive(false);
        SetInteractable(true);
    }

    private void SetStatus(string txt)
    {
        if (statusText) statusText.text = txt;
    }

    private void SetInteractable(bool state)
    {
        if (linkGoogleButton)    linkGoogleButton.interactable = state && linkGoogleButton.interactable;
        if (forceUploadButton)   forceUploadButton.interactable = state;
        if (forceDownloadButton) forceDownloadButton.interactable = state;
        if (refreshButton)       refreshButton.interactable = state;
        if (signOutButton)       signOutButton.interactable = state;
    }

    private async void OnLinkGoogle()
    {
        if (GameServicesInitializer.main == null) return;

        // TODO: integrate actual Google Play Games SDK to get a server auth code
        string serverAuthCode = await AcquireGoogleServerAuthCode();
        if (string.IsNullOrEmpty(serverAuthCode))
        {
            SetStatus("Google auth code failed (stub).");
            return;
        }

        SetStatus("Linking Google...");
        bool ok = await GameServicesInitializer.main.LinkGoogle(serverAuthCode);
        SetStatus(ok ? "Google linked." : "Link failed.");
        Refresh();
    }

    private async System.Threading.Tasks.Task<string> AcquireGoogleServerAuthCode()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!simulateGoogle)
        {
            var (ok, code) = await GooglePlayAuthBridge.SignInAndGetServerAuthCode();
            return ok ? code : "";
        }
#endif
        if (simulateGoogle)
        {
            await System.Threading.Tasks.Task.Delay(200);
            return "SIMULATED_AUTH_CODE";
        }
        return "";
    }

    private void DebugSignOut()
    {
        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
            SetStatus("Signed out (debug). Restart auth flow.");
        }
    }
}
