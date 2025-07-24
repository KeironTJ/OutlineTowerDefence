using UnityEngine;

public static class NumberManager
{
    public static string FormatLargeNumber(float number)
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

        return number.ToString("0.00");
    }
}
