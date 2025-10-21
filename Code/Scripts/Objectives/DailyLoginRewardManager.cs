using UnityEngine;

/// <summary>
/// Legacy bridge maintained for backwards compatibility.
/// Routes daily login calls into the store's daily free prism pack.
/// </summary>
public class DailyLoginRewardManager : MonoBehaviour
{
    public static DailyLoginRewardManager main;

    [SerializeField] private string dailyPackId = "pack_prisms_daily";

    private void Awake()
    {
        if (main != null && main != this)
        {
            Destroy(gameObject);
            return;
        }

        main = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool CanClaimToday()
    {
        var store = StoreService.Instance;
        if (store == null)
            return false;

        var availability = store.GetPrismPackAvailability(dailyPackId);
        return availability.isAvailable;
    }

    public void ClaimToday()
    {
        var store = StoreService.Instance;
        if (store == null)
        {
            Debug.LogWarning("[DailyLoginRewardManager] StoreService unavailable.");
            return;
        }

        if (!store.TryPurchasePrismPack(dailyPackId))
            Debug.LogWarning("[DailyLoginRewardManager] Daily pack claim failed. It may already be claimed for today.");
    }
}
