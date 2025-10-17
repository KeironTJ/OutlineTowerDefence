public class ChipUpgradedEvent
{
    public string chipId;
    public string chipName;
    public int newRarityLevel;
    public ChipRarity newRarity;
    
    public ChipUpgradedEvent(string chipId, string chipName, int newRarityLevel, ChipRarity newRarity)
    {
        this.chipId = chipId;
        this.chipName = chipName;
        this.newRarityLevel = newRarityLevel;
        this.newRarity = newRarity;
    }
}
