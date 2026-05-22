using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển Pause Menu dùng chung cho mobile UI và các scene gameplay.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [Header("Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private InputMode modeBeforePause = InputMode.Map;
    private bool isPaused;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        resumeButton?.onClick.AddListener(Resume);
        saveButton?.onClick.AddListener(SaveGame);
        mainMenuButton?.onClick.AddListener(BackToMainMenu);
        quitButton?.onClick.AddListener(QuitGame);
        SetStatus("");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Toggle()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused || !CanPause())
            return;

        isPaused = true;

        if (InputController.Instance != null)
        {
            modeBeforePause = InputController.Instance.Mode;
            InputController.Instance.SetMode(InputMode.Pause);
        }

        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        SetStatus("");
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (InputController.Instance != null)
            InputController.Instance.SetMode(modeBeforePause);
    }

    public void SaveGame()
    {
        if (GameManager.Instance == null)
        {
            SetStatus("Chưa tìm thấy GameManager.");
            return;
        }

        if (saveButton != null)
            saveButton.interactable = false;

        SetStatus("Đang lưu...");
        GameManager.Instance.SavePlayerPartyWithCallback(OnSaveComplete);
    }

    void BackToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (InputController.Instance != null)
            InputController.Instance.SetMode(InputMode.UI);

        SceneManager.LoadScene("StartScene");
    }

    void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnSaveComplete(bool serverBackupOk)
    {
        if (saveButton != null)
            saveButton.interactable = true;

        SetStatus(serverBackupOk
            ? "Đã lưu game."
            : "Đã lưu trên máy. Chưa backup lên server.");
    }

    bool CanPause()
    {
        if (InputController.Instance == null)
            return true;

        InputMode mode = InputController.Instance.Mode;
        return mode == InputMode.Map
            || mode == InputMode.Battle
            || mode == InputMode.BattleSkillMenu
            || mode == InputMode.BattleItemMenu;
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
