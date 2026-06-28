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
    /// <summary>
    /// Cờ bật log chi tiết (per-frame, debug input...). MẶC ĐỊNH TẮT để không spam
    /// console kể cả trong Editor/Development build. Bật thủ công khi cần debug.
    /// </summary>
    public static bool EnableVerbose = false;

    /// <summary>
    /// Log chi tiết: chỉ in khi EnableVerbose = true, và bị strip hoàn toàn trong release.
    /// Dùng cho các log gọi mỗi frame ([MOBILE-DBG]...) để không làm nghẽn console.
    /// </summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Verbose(string message)
    {
        if (EnableVerbose) UnityEngine.Debug.Log(message);
    }

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
