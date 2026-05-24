using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Overlay mobile: joystick di chuyển + các nút battle.
///
/// SETUP trong Unity:
///   1. Tạo Canvas (Screen Space – Overlay, Scale with Screen Size 1080×1920).
///   2. Gắn script này vào root GameObject của Canvas.
///   3. Tạo panel MapPanel và PausePanel.
///   4. Trong MapPanel: thêm VirtualJoystick (bottom-left) + Button "Tương tác" (bottom-right).
///   5. Tạo PauseButton góc trên phải và kéo các references vào Inspector.
/// </summary>
public class MobileInputUI : MonoBehaviour
{
    public static MobileInputUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Global – Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button battleBackButton;

    [Header("Map – Joystick & Interact")]
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private Button interactButton;

    [Header("Settings")]
    [Tooltip("Bật để test giao diện mobile ngay trong Editor")]
    [SerializeField] private bool forceEnableInEditor = false;
    [Tooltip("Tự tạo nút Back battle nếu prefab chưa wire.")]
    [SerializeField] private bool autoCreateBattleBackButton = true;

    private InputMode lastMode = (InputMode)(-1);
    private PlayerMovement playerMovement;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
#if UNITY_EDITOR
        if (!forceEnableInEditor) { gameObject.SetActive(false); return; }
#elif UNITY_WEBGL
        gameObject.SetActive(false);
        return;
#else
        if (!Application.isMobilePlatform) { gameObject.SetActive(false); return; }
#endif
        DisableLegacyBattleOverlay();
        EnsureBattleBackButton();
        BindButtons();

        if (joystick != null)
            joystick.OnInputChanged += v => playerMovement?.SetMobileInput(v);
    }

    void Update()
    {
        var ic = InputController.Instance;
        if (ic == null) return;

        InputMode mode = ic.Mode;
        if (mode == lastMode) return;

        lastMode = mode;
        RefreshPanels(mode);

        // Cache PlayerMovement khi chuyển sang Map
        if (mode == InputMode.Map || mode == InputMode.Cutscene)
        {
            if (playerMovement == null)
                playerMovement = Object.FindFirstObjectByType<PlayerMovement>();
        }
        else
        {
            // Dừng player khi vào Battle / UI
            playerMovement?.SetMobileInput(Vector2.zero);
        }
    }

    void RefreshPanels(InputMode mode)
    {
        bool isPaused = mode == InputMode.Pause;
        bool isMap    = !isPaused && (mode == InputMode.Map || mode == InputMode.Cutscene);
        bool isBattleFlow = !isPaused && (mode == InputMode.Battle || mode == InputMode.BattleSkillMenu || mode == InputMode.BattleItemMenu);

        if (mapPanel)       mapPanel.SetActive(isMap);
        if (pausePanel)     pausePanel.SetActive(isPaused);
        if (battleBackButton != null) battleBackButton.gameObject.SetActive(isBattleFlow);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(CanShowPauseButton(mode));
    }

    void BindButtons()
    {
        pauseButton?.onClick.AddListener(() => PauseMenuUI.Instance?.Toggle());
        battleBackButton?.onClick.AddListener(() => BattleManager.Instance?.BackToActionMenu());

        // Map
        interactButton?.onClick.AddListener(() =>
            InputController.Instance?.QueueMobileInteract());
    }

    void EnsureBattleBackButton()
    {
        if (!autoCreateBattleBackButton || battleBackButton != null)
            return;

        var go = new GameObject("BattleBackButtonRuntime", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.9f, 0.14f);
        rt.anchorMax = new Vector2(0.9f, 0.14f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 72f);
        rt.anchoredPosition = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.22f, 0.12f, 0.12f, 0.88f);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = "Back";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 30;
        text.color = Color.white;

        battleBackButton = go.GetComponent<Button>();
    }

    void DisableLegacyBattleOverlay()
    {
        // Cách A: Battle dùng UI trong BattleScene, không dùng overlay battle của prefab mobile.
        Transform battle = transform.Find("BattlePanel");
        if (battle != null) battle.gameObject.SetActive(false);

        Transform skill = transform.Find("SkillMenuPanel");
        if (skill != null) skill.gameObject.SetActive(false);
    }

    bool CanShowPauseButton(InputMode mode)
    {
        return mode == InputMode.Map
            || mode == InputMode.Battle
            || mode == InputMode.BattleSkillMenu
            || mode == InputMode.BattleItemMenu;
    }
}
