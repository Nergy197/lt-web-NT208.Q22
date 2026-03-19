using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BattleDebugUI : MonoBehaviour
{
    public static BattleDebugUI Instance;

    public TextMeshProUGUI debugText;

    private const int MAX_LINES = 100;
    private readonly List<string> logLines = new List<string>();

    void Awake()
    {
        Instance = this;
    }

    public void Log(string message)
    {
        logLines.Add(message);

        // Giới hạn số dòng để tránh memory leak
        while (logLines.Count > MAX_LINES)
            logLines.RemoveAt(0);

        debugText.text = string.Join("\n", logLines);
    }

    public void Clear()
    {
        logLines.Clear();
        debugText.text = "";
    }
}