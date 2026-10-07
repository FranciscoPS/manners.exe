using UnityEngine;

public static class SandboxLog
{
    public const string Prefix = "[SANDBOX]";

    public static void Info(string message) => Debug.Log($"{Prefix} {message}");

    public static void Ok(string message) => Debug.Log($"{Prefix} ✔ {message}");

    public static void Skipped(string message) => Debug.Log($"{Prefix} — {message}");

    public static void Warn(string message) => Debug.LogWarning($"{Prefix} ⚠ {message}");

    public static void Error(string message) => Debug.LogError($"{Prefix} ✖ {message}");

    public static void Command(string message) => Debug.Log($"{Prefix} ⌨ {message}");
}
