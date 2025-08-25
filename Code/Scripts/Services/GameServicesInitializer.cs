using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class GameServicesInitializer : MonoBehaviour
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
}