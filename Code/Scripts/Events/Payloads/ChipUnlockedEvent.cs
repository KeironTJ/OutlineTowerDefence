public class ChipUnlockedEvent
{
    public string chipId;
    public string chipName;
    
    public ChipUnlockedEvent(string chipId, string chipName)
    {
        this.chipId = chipId;
        this.chipName = chipName;
    }
}
