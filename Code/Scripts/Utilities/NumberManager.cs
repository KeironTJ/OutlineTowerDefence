using UnityEngine;

/// <summary>
/// Provides utility methods for formatting large numbers for display.
/// </summary>
public static class NumberManager
{
    /// <summary>
    /// Formats a large number into a human-readable string with appropriate suffixes (e.g., k, M, B).
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <param name="isInteger">If true, formats the number with no decimals; otherwise, uses two decimal places.</param>
    /// <returns>A formatted string representing the number with a suffix if applicable.</returns>
    public static string FormatLargeNumber(float number, bool isInteger = false)
    {
        if (number >= 1e33)
            return (number / 1e33).ToString("0.00") + "Dc"; // Decillion
        if (number >= 1e30)
            return (number / 1e30).ToString("0.00") + "No"; // Nonillion
        if (number >= 1e27)
            return (number / 1e27).ToString("0.00") + "Oc"; // Octillion
        if (number >= 1e24)
            return (number / 1e24).ToString("0.00") + "Sp"; // Septillion
        if (number >= 1e21)
            return (number / 1e21).ToString("0.00") + "Sx"; // Sextillion
        if (number >= 1e18)
            return (number / 1e18).ToString("0.00") + "Qi"; // Quintillion
        if (number >= 1e15)
            return (number / 1e15).ToString("0.00") + "Qa"; // Quadrillion
        if (number >= 1e12)
            return (number / 1e12).ToString("0.00") + "T";  // Trillion
        if (number >= 1e9)
            return (number / 1e9).ToString("0.00") + "B";  // Billion
        if (number >= 1e6)
            return (number / 1e6).ToString("0.00") + "M";  // Million
        if (number >= 1e3)
            return (number / 1e3).ToString("0.00") + "k";  // Thousand

        if (isInteger)
            return number.ToString("0");
        else
            return number.ToString("0.00");
    }
}
