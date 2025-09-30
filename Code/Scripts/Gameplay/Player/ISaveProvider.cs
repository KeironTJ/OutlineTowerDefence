using System.Threading.Tasks;

public interface ISaveProvider
{
    Task<bool> ExistsAsync(string slot);
    Task<PlayerSavePayload> LoadAsync(string slot);
    Task<bool> SaveAsync(string slot, PlayerSavePayload payload);
    Task<bool> DeleteAsync(string slot);
}