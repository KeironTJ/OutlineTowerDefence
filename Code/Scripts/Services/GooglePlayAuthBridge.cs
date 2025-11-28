using System.Threading.Tasks;

#if !UNITY_ANDROID || UNITY_EDITOR
public static class GooglePlayAuthBridge
{
    // Matches: var (ok, code) = await GooglePlayAuthBridge.SignInAndGetServerAuthCode();
    public static Task<(bool ok, string code)> SignInAndGetServerAuthCode()
    {
        // development stub – returns failure so calling code falls back to simulateGoogle path
        return Task.FromResult((false, string.Empty));
    }
}
#endif