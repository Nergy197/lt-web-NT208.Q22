using UnityEngine;
using System.Diagnostics;

/// <summary>
/// Wrapper cho Debug.Log, tự động bỏ qua trong Release build.
/// Sử dụng [Conditional("UNITY_EDITOR")] hoặc [Conditional("DEVELOPMENT_BUILD")]
/// để compiler loại bỏ các lời gọi log trong production build.
///
/// Cách dùng: thay Debug.Log("...") bằng GameLog.Log("...");
/// Hoặc thêm #if UNITY_EDITOR trước các dòng Debug.Log quan trọng.
/// </summary>
public static class GameLog
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    // LogError luôn hiện kể cả trong production (lỗi nghiêm trọng cần biết)
    public static void LogError(string message)
    {
        UnityEngine.Debug.LogError(message);
    }
}
