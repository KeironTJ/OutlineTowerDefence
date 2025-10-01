# Utility Classes

This directory contains reusable utility classes extracted during code review to improve modularity and reduce code duplication.

## HashUtility

**Purpose:** Centralized hashing operations  
**Usage:** `string hash = HashUtility.MD5Hash(myString);`  
**Note:** MD5 is used for checksums only, not security-critical operations

## RenderUtility

**Purpose:** Unity rendering utilities (Editor-only)  
**Usage:** `Bounds bounds = RenderUtility.CalculateBounds(gameObject);`  
**Scope:** Only available in Unity Editor (`#if UNITY_EDITOR`)

## RetryUtility

**Purpose:** Async retry logic with exponential backoff  
**Usage:** 
```csharp
var result = await RetryUtility.RetryAsync(
    () => MyNetworkOperation(),
    attempts: 5,
    initialDelayMs: 400,
    logPrefix: "MyFeature");
```

## SlotTimeUtility

**Purpose:** Time-slot calculations for daily/hourly systems  
**Usage:**
```csharp
// Get current slot key (e.g., "20240101-12" for 12:00 slot)
string key = SlotTimeUtility.CurrentSlotKey(6); // 6-hour slots

// Calculate time until next slot
TimeSpan remaining = SlotTimeUtility.GetTimeUntilNextSlot(DateTime.UtcNow, 6);

// Format as countdown string
string countdown = SlotTimeUtility.FormatTimeRemaining(remaining); // "05:23:41"
```

## Design Principles

All utilities follow these principles:
- **Static classes:** No state, pure utility functions
- **Single Responsibility:** Each utility has one clear purpose
- **Reusability:** Can be used throughout the codebase
- **Testability:** Easy to unit test in isolation
- **Documentation:** Clear XML comments on public methods

## Testing

While these utilities don't have automated tests yet, they can be easily unit tested:

- **HashUtility:** Test with various inputs (null, empty, Unicode, large strings)
- **RenderUtility:** Test with different GameObject hierarchies
- **RetryUtility:** Mock operations that fail N times, verify backoff timing
- **SlotTimeUtility:** Test edge cases (midnight, month boundaries, DST, leap years)
