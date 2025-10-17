public class ChipEquippedEvent
{
    public int slotIndex;
    public string chipId;
    public string chipName;
    
    public ChipEquippedEvent(int slotIndex, string chipId, string chipName)
    {
        this.slotIndex = slotIndex;
        this.chipId = chipId;
        this.chipName = chipName;
    }
}
