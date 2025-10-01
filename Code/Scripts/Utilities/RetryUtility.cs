using System;
using System.Threading.Tasks;
using UnityEngine;

public static class RetryUtility
{
    /// <summary>
    /// Retry an async operation with exponential backoff.
    /// </summary>
    /// <param name="operation">The async operation to retry</param>
    /// <param name="attempts">Maximum number of attempts (default 3)</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds (default 400)</param>
    /// <param name="logPrefix">Prefix for log messages (optional)</param>
    public static async Task<T> RetryAsync<T>(
        Func<Task<T>> operation, 
        int attempts = 3, 
        int initialDelayMs = 400,
        string logPrefix = null)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));
        
        if (attempts < 1)
            throw new ArgumentOutOfRangeException(nameof(attempts), "Must be at least 1");
        
        int delay = initialDelayMs;
        Exception lastException = null;
        string prefix = string.IsNullOrEmpty(logPrefix) ? "[Retry]" : $"[{logPrefix}]";
        
        for (int i = 1; i <= attempts; i++)
        {
            try 
            { 
                return await operation(); 
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (i == attempts) 
                    throw;
                
                Debug.LogWarning($"{prefix} Attempt {i}/{attempts} failed: {ex.Message}");
                await Task.Delay(delay);
                delay *= 2; // exponential backoff
            }
        }
        
        // This should never be reached due to throw above, but for safety
        throw lastException ?? new Exception("Retry failed with unknown error");
    }
}
