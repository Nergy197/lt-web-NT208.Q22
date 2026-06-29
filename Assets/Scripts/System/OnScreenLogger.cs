using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hiện log Debug.Log NGAY TRÊN MÀN HÌNH (cho APK/mobile — nơi không xem console được).
/// Tự khởi tạo, lọc các tag đang quan tâm ([MOBILE-TAP], [PAUSE]) + Warning/Error.
/// Bật/tắt bằng cờ Enabled. Xoá file này khi không cần nữa.
/// </summary>
public class OnScreenLogger : MonoBehaviour
{
    public static bool Enabled = true; // đặt false để tắt overlay

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (!Enabled) return;
        var go = new GameObject("[OnScreenLogger]");
        DontDestroyOnLoad(go);
        go.AddComponent<OnScreenLogger>();
    }

    readonly Queue<string> _lines = new Queue<string>();
    const int MaxLines = 14;
    GUIStyle _style;

    void OnEnable()  => Application.logMessageReceived += OnLog;
    void OnDisable() => Application.logMessageReceived -= OnLog;

    void OnLog(string msg, string stack, LogType type)
    {
        // Chỉ giữ log quan tâm để khỏi tràn màn hình.
        bool keep = type == LogType.Error || type == LogType.Exception || type == LogType.Warning
            || msg.Contains("[MOBILE-TAP]") || msg.Contains("[PAUSE]") || msg.Contains("[TARGET]");
        if (!keep) return;

        _lines.Enqueue(msg.Length > 90 ? msg.Substring(0, 90) : msg);
        while (_lines.Count > MaxLines) _lines.Dequeue();
    }

    void OnGUI()
    {
        if (!Enabled || _lines.Count == 0) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.022f)),
                wordWrap = false
            };
        }

        float lineH = _style.fontSize + 4f;
        // nền tối mờ để dễ đọc
        GUI.color = new Color(0, 0, 0, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, lineH * _lines.Count + 8), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float y = 4;
        foreach (var line in _lines)
        {
            _style.normal.textColor = line.Contains("[MOBILE-TAP]") ? Color.cyan
                : (line.Contains("error") || line.Contains("Error") || line.Contains("LỖI")) ? new Color(1f, 0.5f, 0.5f)
                : Color.yellow;
            GUI.Label(new Rect(6, y, Screen.width - 12, lineH), line, _style);
            y += lineH;
        }
    }
}
