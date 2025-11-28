using UnityEngine;
public static class Log
{
    public static bool verbose = true;
    public static void Cloud(string msg) { if (verbose) Debug.Log("[Cloud] " + msg); }
    public static void Auth(string msg)  { if (verbose) Debug.Log("[Auth] " + msg); }
    public static void Save(string msg)  { if (verbose) Debug.Log("[Save] " + msg); }
}
