using UnityEngine;
using TMPro;

public class BattleDebugUI : MonoBehaviour
{
    public static BattleDebugUI Instance;

    public TextMeshProUGUI debugText;

    string log = "";

    void Awake()
    {
        Instance = this;
    }

    public void Log(string message)
    {
        log += message + "\n";

        debugText.text = log;
    }

    public void Clear()
    {
        log = "";
        debugText.text = "";
    }
}