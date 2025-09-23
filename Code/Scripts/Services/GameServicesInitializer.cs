using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public partial class GameServicesInitializer : MonoBehaviour
{
    public static GameServicesInitializer main;
    public bool Initialized { get; private set; }
    public bool SignedIn => AuthenticationService.Instance?.IsSignedIn ?? false;

    private async void Awake()
    {
        if (main != null && main != this) { Destroy(gameObject); return; }
        main = this;
        DontDestroyOnLoad(gameObject);
        await InitAndSignInAsync();
    }

    public async Task InitAndSignInAsync()
    {
        if (!Initialized)
        {
            await UnityServices.InitializeAsync();
            Initialized = true;
        }
        if (!SignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("UGS Signed In: " + AuthenticationService.Instance.PlayerId);
        }
    }

    public async Task<bool> SignInGoogleDirect(string serverAuthCode)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(serverAuthCode);
            Debug.Log("[Auth] Google direct sign-in success PlayerId=" + AuthenticationService.Instance.PlayerId);
            return true;
        }
        catch (AuthenticationException e)
        {
            Debug.LogWarning("[Auth] Google direct sign-in failed: " + e);
            return false;
        }
    }

    public async Task<bool> LinkGoogle(string serverAuthCode)
    {
        if (!SignedIn) return false;
        try
        {
            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(serverAuthCode);
            Debug.Log("[Auth] Google linked.");
            return true;
        }
        catch (AuthenticationException e)
        {
            Debug.LogWarning("[Auth] Google link failed: " + e);
            return false;
        }
    }

    public const string ProviderGooglePlayGames = "googleplaygames";
    public const string ProviderApple          = "apple";
    public const string ProviderSteam          = "steam";
    public const string ProviderFacebook       = "facebook";
    public const string ProviderCustom         = "custom"; // when using custom ID

    public bool HasProvider(string providerId)
    {
        var info = Unity.Services.Authentication.AuthenticationService.Instance.PlayerInfo;
        if (info == null) return false;

        // Newer SDK exposes Identities list
        var identitiesProp = info.GetType().GetProperty("Identities");
        if (identitiesProp != null)
        {
            var identities = identitiesProp.GetValue(info) as System.Collections.IEnumerable;
            if (identities != null)
            {
                foreach (var id in identities)
                {
                    var typeIdProp = id.GetType().GetProperty("TypeId");
                    var typeId = typeIdProp != null ? typeIdProp.GetValue(id) as string : null;
                    if (!string.IsNullOrEmpty(typeId) &&
                        string.Equals(typeId, providerId, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        return false;
    }
}