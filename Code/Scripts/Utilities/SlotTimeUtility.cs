using System;
using System.Globalization;

public static class SlotTimeUtility
{
    /// <summary>
    /// Get the start time of the current time slot.
    /// </summary>
    public static DateTime GetSlotStartForTime(DateTime utcTime, int slotLengthHours)
    {
        if (slotLengthHours <= 0)
            throw new ArgumentOutOfRangeException(nameof(slotLengthHours), "Must be positive");
        
        int slotStartHour = (utcTime.Hour / slotLengthHours) * slotLengthHours;
        return new DateTime(
            utcTime.Year, 
            utcTime.Month, 
            utcTime.Day, 
            slotStartHour, 
            0, 
            0, 
            DateTimeKind.Utc);
    }
    
    /// <summary>
    /// Generate a slot key from a slot start time.
    /// Format: yyyyMMdd-HH
    /// </summary>
    public static string SlotKeyFromStart(DateTime slotStartUtc)
    {
        return slotStartUtc.ToString("yyyyMMdd") + "-" + slotStartUtc.Hour.ToString("00");
    }
    
    /// <summary>
    /// Generate a slot key for the current time.
    /// </summary>
    public static string CurrentSlotKey(int slotLengthHours)
    {
        return CurrentSlotKey(DateTime.UtcNow, slotLengthHours);
    }
    
    /// <summary>
    /// Generate a slot key for a specific time.
    /// </summary>
    public static string CurrentSlotKey(DateTime utcTime, int slotLengthHours)
    {
        int slotIndex = utcTime.Hour / slotLengthHours;
        int slotStartHour = slotIndex * slotLengthHours;
        return utcTime.ToString("yyyyMMdd") + "-" + slotStartHour.ToString("00");
    }
    
    /// <summary>
    /// Parse a slot key into a DateTime.
    /// </summary>
    public static bool TryParseSlotKey(string key, int slotLengthHours, out DateTime slotStartUtc)
    {
        slotStartUtc = DateTime.MinValue;
        
        if (string.IsNullOrEmpty(key))
            return false;
        
        // Expected format: yyyyMMdd-HH
        var parts = key.Split('-');
        if (parts.Length != 2)
            return false;
        
        if (!DateTime.TryParseExact(
            parts[0],
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime day))
            return false;
        
        if (!int.TryParse(parts[1], out int hour))
            return false;
        
        if (hour < 0 || hour >= 24)
            return false;
        
        // Snap hour to slot boundary (defensive)
        int slotStartHour = (hour / slotLengthHours) * slotLengthHours;
        
        slotStartUtc = new DateTime(
            day.Year, 
            day.Month, 
            day.Day, 
            slotStartHour, 
            0, 
            0, 
            DateTimeKind.Utc);
        
        return true;
    }
    
    /// <summary>
    /// Get the start time of the next slot.
    /// </summary>
    public static DateTime GetNextSlotStart(DateTime currentSlotStart, int slotLengthHours)
    {
        if (slotLengthHours <= 0)
            return currentSlotStart; // safety
        
        return currentSlotStart.AddHours(slotLengthHours);
    }
    
    /// <summary>
    /// Calculate time remaining until the next slot.
    /// </summary>
    public static TimeSpan GetTimeUntilNextSlot(DateTime utcNow, int slotLengthHours)
    {
        var currentStart = GetSlotStartForTime(utcNow, slotLengthHours);
        var nextStart = GetNextSlotStart(currentStart, slotLengthHours);
        return nextStart - utcNow;
    }
    
    /// <summary>
    /// Format time remaining as HH:MM:SS
    /// </summary>
    public static string FormatTimeRemaining(TimeSpan remaining)
    {
        if (remaining.TotalSeconds < 0) 
            remaining = TimeSpan.Zero;
        
        return $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}";
    }
}
