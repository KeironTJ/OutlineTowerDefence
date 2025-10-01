using System;
using System.Security.Cryptography;
using System.Text;

public static class HashUtility
{
    /// <summary>
    /// Compute MD5 hash of a string.
    /// </summary>
    public static string MD5Hash(string input)
    {
        if (string.IsNullOrEmpty(input)) 
            return string.Empty;
        
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}

