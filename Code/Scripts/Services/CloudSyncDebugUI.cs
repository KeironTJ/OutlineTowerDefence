using UnityEngine;
using UnityEngine.UI;

public class CloudSyncDebugUI : MonoBehaviour
{
    [SerializeField] Button forceUploadBtn;
    [SerializeField] Button forceDownloadBtn;
    [SerializeField] TMPro.TextMeshProUGUI status;

    private void Awake()
    {
        if (forceUploadBtn) forceUploadBtn.onClick.AddListener(() => _ = ForceUpload());
        if (forceDownloadBtn) forceDownloadBtn.onClick.AddListener(() => _ = ForceDownload());
    }

    private void Update()
    {
        if (status && GameServicesInitializer.main)
            status.text = GameServicesInitializer.main.SignedIn ? "Cloud Auth: OK" : "Cloud Auth: OFF";
    }

    private async System.Threading.Tasks.Task ForceUpload()
    {
        await System.Threading.Tasks.Task.Yield();
        CloudSyncService.main?.ForceUploadNow();
    }

    private async System.Threading.Tasks.Task ForceDownload()
    {
        await System.Threading.Tasks.Task.Yield();
        CloudSyncService.main?.ForceDownloadNow();
    }
}