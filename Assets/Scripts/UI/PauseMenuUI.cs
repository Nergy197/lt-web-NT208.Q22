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
    public static bool IsPaused { get; private set; }
    private const int PauseSortingOrder = 30000;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
        IsPaused = false;
    }

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
        {
            EnsurePausePanelOnTop();
            pausePanel.SetActive(false);
        }

        resumeButton?.onClick.AddListener(Resume);
        saveButton?.onClick.AddListener(SaveGame);
        mainMenuButton?.onClick.AddListener(BackToMainMenu);
        quitButton?.onClick.AddListener(QuitGame);
        SetStatus("");
    }

    void Start()
    {
        // Luôn đảm bảo có UI runtime (panel + nút pause) — không phụ thuộc việc scene
        // có gán sẵn hay không. Idempotent nhờ cờ _runtimeBuilt.
        BuildRuntimeUI();
    }

    private bool _runtimeBuilt;

    // Các nút runtime + hành động, xử lý bằng POLL chuột/touch trong Update — không phụ
    // thuộc EventSystem/GraphicRaycaster (trên WebGL click UGUI tới UI tạo runtime đôi khi
    // không ăn). Mỗi entry chỉ kích hoạt khi nút đang active (panel mở thì nút panel mới ăn).
    private readonly System.Collections.Generic.List<(RectTransform rect, System.Action act)> _pollButtons
        = new System.Collections.Generic.List<(RectTransform, System.Action)>();

    void Update()
    {
        if (_pollButtons.Count == 0) return;

        if (!TryGetClickPos(out Vector2 pos)) return;

        foreach (var b in _pollButtons)
        {
            if (b.rect == null || !b.rect.gameObject.activeInHierarchy) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(b.rect, pos, null))
            {
                b.act?.Invoke();
                return; // 1 click = 1 nút
            }
        }
    }

    static bool TryGetClickPos(out Vector2 pos)
    {
        pos = Vector2.zero;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pos = mouse.position.ReadValue();
            return true;
        }
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
        {
            pos = ts.primaryTouch.position.ReadValue();
            return true;
        }
        return false;
    }

    /// <summary>Tìm Canvas ScreenSpaceOverlay gốc đang hoạt động (để UI nhận click qua EventSystem).</summary>
    static Canvas FindOverlayCanvas()
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;
        return null;
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
        IsPaused = true;

        if (InputController.Instance != null)
        {
            modeBeforePause = InputController.Instance.Mode;
            InputController.Instance.SetMode(InputMode.Pause);
        }

        Time.timeScale = 0f;

        SFXManager.Instance?.PlayPanelOpen();

        if (pausePanel != null)
        {
            EnsurePausePanelOnTop();
            pausePanel.SetActive(true);
        }

        SetStatus("");
    }

    public void Resume()
    {
        if (!isPaused)
            return;

        isPaused = false;
        IsPaused = false;
        Time.timeScale = 1f;

        SFXManager.Instance?.PlayPanelClose();

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
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Giữ Map mode (không dùng UI) để InputSystemUIInputModule
        // vẫn nhận pointer events khi Chapter0_Login load xong.
        if (InputController.Instance != null)
            InputController.Instance.SetMode(InputMode.Map);

        SceneManager.LoadScene("Chapter0_Login");
    }

    void QuitGame()
    {
        if (IsBattleContext())
        {
            AbandonBattleToMap();
            return;
        }

        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    bool IsBattleContext()
    {
        if (InputController.Instance != null)
        {
            InputMode mode = InputController.Instance.Mode;
            if (mode == InputMode.Battle
                || mode == InputMode.BattleSkillMenu
                || mode == InputMode.BattleItemMenu)
                return true;
        }

        return SceneManager.GetActiveScene().name == "Chapter5a_Battle";
    }

    void AbandonBattleToMap()
    {
        isPaused = false;
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (InputController.Instance != null)
            InputController.Instance.UnbindBattleManager();

        if (MapManager.Instance != null)
        {
            MapManager.Instance.AbandonBattleToMap();
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] MapManager missing, fallback load Chapter5_MapBattle.");
            SceneManager.LoadScene("Chapter5_MapBattle");
        }
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

    void EnsurePausePanelOnTop()
    {
        if (pausePanel == null)
            return;

        pausePanel.transform.SetAsLastSibling();

        Canvas canvas = pausePanel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = pausePanel.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = PauseSortingOrder;

        if (pausePanel.GetComponent<GraphicRaycaster>() == null)
            pausePanel.AddComponent<GraphicRaycaster>();
    }

    /// <summary>Tạo PauseMenuUI nếu scene chưa có (panel sẽ tự dựng trong Start).</summary>
    public static void EnsureExists()
    {
        if (Instance != null) return;
        new GameObject("PauseMenuUI").AddComponent<PauseMenuUI>();
        Debug.Log("[PauseMenuUI] Đã tạo PauseMenuUI runtime.");
    }

    // ================= RUNTIME PANEL BUILDER =================

    /// <summary>
    /// Dựng panel Pause bằng code khi scene chưa gán (đặc biệt cho bản web).
    /// Tạo overlay + card + 4 nút (Tiếp tục / Lưu / Menu chính / Thoát) + status text.
    /// </summary>
    public void BuildRuntimeUI()
    {
        if (_runtimeBuilt && pausePanel != null) return;
        _runtimeBuilt = true;

        // Ưu tiên dùng Canvas ScreenSpaceOverlay SẴN CÓ (đang hoạt động cho UI khác →
        // chắc chắn nhận click qua EventSystem). Chỉ tự tạo nếu scene không có.
        Canvas canvas = FindOverlayCanvas();
        if (canvas == null)
        {
            var canvasGo = new GameObject("PauseRuntimeCanvas",
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = PauseSortingOrder;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        var overlay = NewRect("PausePanel", canvas.transform);
        var oImg = overlay.gameObject.AddComponent<Image>();
        oImg.color = new Color(0, 0, 0, 0.8f);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero; overlay.offsetMax = Vector2.zero;

        var card = NewRect("Card", overlay);
        card.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.98f);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(420, 380);

        MakeLabel(card, "TẠM DỪNG", new Vector2(0, 150), 28, true, new Color(0.8f, 0.9f, 1f));
        var status = MakeLabel(card, "", new Vector2(0, 110), 14, false, new Color(0.85f, 0.9f, 0.96f));

        var resumeBtn  = MakeButton(card, "Tiếp tục",   new Vector2(0, 56),  new Color(0.12f, 0.45f, 0.25f));
        var saveBtn    = MakeButton(card, "Lưu game",   new Vector2(0, 4),   new Color(0.12f, 0.35f, 0.6f));
        var menuBtn    = MakeButton(card, "Menu chính", new Vector2(0, -48), new Color(0.3f, 0.3f, 0.4f));
        var quitBtn    = MakeButton(card, "Thoát",      new Vector2(0, -110), new Color(0.5f, 0.15f, 0.15f));

        pausePanel = overlay.gameObject;
        statusText = status;
        resumeButton = resumeBtn;
        saveButton = saveBtn;
        mainMenuButton = menuBtn;
        quitButton = quitBtn;

        // Xử lý click bằng POLL (xem Update), KHÔNG dùng onClick → tránh phụ thuộc
        // EventSystem và tránh double-fire. Mỗi nút chỉ ăn click khi đang active.
        _pollButtons.Add((resumeBtn.GetComponent<RectTransform>(), Resume));
        _pollButtons.Add((saveBtn.GetComponent<RectTransform>(),   SaveGame));
        _pollButtons.Add((menuBtn.GetComponent<RectTransform>(),   BackToMainMenu));
        _pollButtons.Add((quitBtn.GetComponent<RectTransform>(),   QuitGame));

        // Panel hiển thị trên cùng của canvas riêng (sortingOrder cao sẵn).
        pausePanel.transform.SetAsLastSibling();
        pausePanel.SetActive(false);

        // Nút Pause LUÔN HIỆN ở góc trên-TRÁI (chỗ trống, tránh đè minimap góc phải) —
        // bấm chuột, không phụ thuộc ESC (trình duyệt web thường nuốt phím ESC).
        var pbRt = NewRect("PauseButton", canvas.transform);
        pbRt.anchorMin = pbRt.anchorMax = pbRt.pivot = new Vector2(0, 1);
        pbRt.anchoredPosition = new Vector2(16, -16);
        pbRt.sizeDelta = new Vector2(64, 64);
        pbRt.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.2f, 0.95f);
        pbRt.gameObject.AddComponent<Button>(); // hiệu ứng nhấn; hành động xử lý ở Update (poll)
        pbRt.SetAsLastSibling(); // nổi trên UI khác
        _pollButtons.Add((pbRt, Toggle)); // poll chuột/touch — không phụ thuộc EventSystem

        var pbLbl = NewRect("Lbl", pbRt);
        pbLbl.anchorMin = Vector2.zero; pbLbl.anchorMax = Vector2.one;
        pbLbl.offsetMin = pbLbl.offsetMax = Vector2.zero;
        var pbT = pbLbl.gameObject.AddComponent<TextMeshProUGUI>();
        pbT.text = "II"; pbT.fontSize = 28; pbT.color = Color.white;
        pbT.alignment = TextAlignmentOptions.Center;
        pbT.fontStyle = FontStyles.Bold;

        Debug.Log("[PauseMenuUI] Đã dựng panel pause + nút pause runtime (canvas riêng).");
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static TMP_Text MakeLabel(Transform parent, string text, Vector2 pos, int size, bool bold, Color color)
    {
        var rt = NewRect("Lbl", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(380, 40);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return t;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Color bg)
    {
        var rt = NewRect("Btn", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(300, 44);
        rt.gameObject.AddComponent<Image>().color = bg;
        var btn = rt.gameObject.AddComponent<Button>();

        var trt = NewRect("Lbl", rt);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = trt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 18; t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        return btn;
    }
}
