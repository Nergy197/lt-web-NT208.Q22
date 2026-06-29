using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private Button battleConfirmButton;
    [SerializeField] private Button battleParryButton;

    [Header("Map – Joystick & Interact")]
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private Button interactButton;

    [Header("Scene Layout Presets")]
    [Tooltip("Thư mục chứa preset layout per-scene (trong Resources). Để trống để dùng layout prefab cố định.")]
    [SerializeField] private string layoutPresetFolder = "MobileLayouts";
    [Tooltip("Tự động load preset layout tương ứng với scene đang active.")]
    [SerializeField] private bool autoApplySceneLayout = true;

    [Header("Settings")]
    [Tooltip("Bật để test giao diện mobile ngay trong Editor")]
    [SerializeField] private bool forceEnableInEditor = false;
    [Tooltip("Tự tạo nút Back battle nếu prefab chưa wire.")]
    [SerializeField] private bool autoCreateBattleBackButton = true;
    [Tooltip("Tự tạo các nút tắt battle còn thiếu trên mobile.")]
    [SerializeField] private bool autoCreateBattleShortcutButtons = true;
    [Header("Mobile Button Layout")]
    [Tooltip("Bật nếu muốn script ép vị trí theo các Anchor bên dưới. Tắt để giữ vị trí kéo tay trong prefab.")]
    [SerializeField] private bool applyConfiguredButtonLayout = false;
    [SerializeField] private Vector2 interactAnchor = new Vector2(0.88f, 0.10f);
    [SerializeField] private Vector2 backAnchor     = new Vector2(0.12f, 0.10f); // góc trái dưới, tránh đè lên enemy (luôn ở phải)
    [SerializeField] private Vector2 confirmAnchor  = new Vector2(0.88f, 0.10f);
    [SerializeField] private Vector2 parryAnchor    = new Vector2(0.88f, 0.28f);
    [SerializeField] private Vector2 buttonSize = new Vector2(150f, 72f);

    private InputMode lastMode = (InputMode)(-1);
    private PlayerMovement playerMovement;
    private PlayerMovement_Cutscene cutsceneMovement;
    private bool createdRuntimeButton;
    private bool enemyAttackIncoming = false; // true từ lúc enemy bắt đầu tấn công đến khi xong

#if UNITY_EDITOR
    // Tự tạo MobileInputCanvas nếu scene hiện tại không có (tiện test từ bất kỳ scene nào).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EditorAutoBootstrap()
    {
        if (Instance != null) return;
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/Mobile/MobileInputCanvas.prefab");
        if (prefab != null)
            Instantiate(prefab);
    }
#endif

    // Cache preset per scene name để tránh load lại mỗi frame
    readonly Dictionary<string, MobileSceneLayout> _presetCache = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
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
        EnsureBattleShortcutButtons();
        if (applyConfiguredButtonLayout || createdRuntimeButton)
            ApplyMobileButtonLayout();
        // Luôn KHOÁ nút Parry vào góc dưới-phải (kể cả khi không áp layout cấu hình) — tránh
        // bị khuất mép phải màn hình.
        LayoutBottomRight(battleParryButton, buttonSize);
        BindButtons();

        if (joystick != null)
            joystick.OnInputChanged += SetMobileMovement;

        BattleEvents.OnEnemyAttackAnnounced += OnEnemyAttackAnnounced;
        BattleEvents.OnAttackFinished        += OnAttackFinished;

        // Tắt Canvas/GR ngay nếu scene hiện tại không cần mobile UI
        SetCanvasActive(!ShouldHideGameplayOverlay());
    }

    void OnDestroy()
    {
        BattleEvents.OnEnemyAttackAnnounced -= OnEnemyAttackAnnounced;
        BattleEvents.OnAttackFinished        -= OnAttackFinished;
        SceneManager.sceneLoaded            -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool excluded = scene.name == "Chapter0_Login" || scene.name == "Chapter1_Introduction" || scene.name == "Chapter1_CutScene" || scene.name == "Chapter4_WarNews" || scene.name == "Chapter6_Village" || scene.name == "Chapter6_Ending";
        // Tắt hoàn toàn Canvas + GraphicRaycaster ở scene không dùng mobile UI
        // để tránh block input của scene đó.
        SetCanvasActive(!excluded);

        if (!excluded && autoApplySceneLayout)
            ApplySceneLayoutPreset(scene.name);
    }

    void SetCanvasActive(bool active)
    {
        var canvas = GetComponent<Canvas>();
        if (canvas) canvas.enabled = active;
        var gr = GetComponent<GraphicRaycaster>();
        if (gr) gr.enabled = active;
    }

    // ── Per-scene layout preset ───────────────────────────────────────────────

    void ApplySceneLayoutPreset(string sceneName)
    {
        if (!_presetCache.TryGetValue(sceneName, out var preset))
        {
            // Load từ Resources/MobileLayouts/MobileLayout_<SceneName>
            string resPath = string.IsNullOrEmpty(layoutPresetFolder)
                ? $"MobileLayout_{sceneName}"
                : $"{layoutPresetFolder}/MobileLayout_{sceneName}";
            preset = Resources.Load<MobileSceneLayout>(resPath);
            _presetCache[sceneName] = preset; // null cũng cache để không load lại
        }

        if (preset == null) return;

        // Apply CanvasScaler
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(preset.referenceResolution.x, preset.referenceResolution.y);
            scaler.matchWidthOrHeight  = preset.matchWidthOrHeight;
        }

        // Apply từng element
        foreach (var el in preset.elements)
        {
            var t = el.path == "" ? transform : transform.Find(el.path);
            if (t == null) continue;

            t.gameObject.SetActive(el.active);

            var rt = t.GetComponent<RectTransform>();
            if (rt == null) continue;

            rt.anchorMin        = el.anchorMin;
            rt.anchorMax        = el.anchorMax;
            rt.pivot            = el.pivot;
            rt.anchoredPosition = el.position;
            rt.sizeDelta        = el.size;
        }
    }

    void OnEnemyAttackAnnounced(EnemyAttackData attack, EnemyStatus enemy, PlayerStatus target)
        => enemyAttackIncoming = true;

    void OnAttackFinished()
        => enemyAttackIncoming = false;

    void Update()
    {
        var ic = InputController.Instance;
        if (ic == null) return;

        InputMode mode = ic.Mode;
        RefreshPanels(mode);

        if (mode == lastMode) return;
        lastMode = mode;

        // Cache PlayerMovement khi chuyển sang Map
        if (mode == InputMode.Map || mode == InputMode.Cutscene)
        {
            CacheMovementTargets();
        }
        else
        {
            // Dừng player khi vào Battle / UI
            SetMobileMovement(Vector2.zero);
        }
    }

    void RefreshPanels(InputMode mode)
    {
        if (ShouldHideGameplayOverlay())
        {
            if (mapPanel) mapPanel.SetActive(false);
            if (pausePanel) pausePanel.SetActive(false);
            if (pauseButton != null) pauseButton.gameObject.SetActive(false);
            if (interactButton != null) interactButton.gameObject.SetActive(false);
            if (battleBackButton != null) battleBackButton.gameObject.SetActive(false);
            if (battleConfirmButton != null) battleConfirmButton.gameObject.SetActive(false);
            if (battleParryButton != null) battleParryButton.gameObject.SetActive(false);
            return;
        }

        bool isPaused = mode == InputMode.Pause;
        bool isMap    = !isPaused && (mode == InputMode.Map || mode == InputMode.Cutscene);
        bool isInSkillOrItemMenu = !isPaused && (mode == InputMode.BattleSkillMenu || mode == InputMode.BattleItemMenu);
        bool isSelectingTarget = !isPaused
            && mode == InputMode.Battle
            && BattleManager.Instance != null
            && BattleManager.Instance.IsSelectingTarget;
        // Confirm chỉ hiện SAU khi tap 1 đã chọn được target — tránh nút chặn click chọn enemy
        bool shouldShowBattleConfirm = isSelectingTarget
            && BattleManager.Instance.IsMobileTargetSelected;
        bool shouldShowBattleBack = isInSkillOrItemMenu || isSelectingTarget;
        // Hiện nút Parry từ khi địch bắt đầu tấn công (không chỉ khi window mở).
        // RequestParryMobile() sẽ queue nếu bấm trước window — không bị penalty.
        bool shouldShowBattleParry = !isPaused
            && mode == InputMode.Battle
            && BattleManager.Instance != null
            && !isSelectingTarget
            && enemyAttackIncoming;

        bool shouldShowInteract = isMap && MobileInteractRegistry.HasActiveInteraction;

        if (mapPanel)       mapPanel.SetActive(isMap);
        if (joystick != null) joystick.gameObject.SetActive(isMap && CanUseJoystick());
        if (interactButton != null) interactButton.gameObject.SetActive(shouldShowInteract);
        if (pausePanel)     pausePanel.SetActive(isPaused);
        if (battleBackButton != null) battleBackButton.gameObject.SetActive(shouldShowBattleBack);
        if (battleConfirmButton != null) battleConfirmButton.gameObject.SetActive(shouldShowBattleConfirm);
        if (battleParryButton != null) battleParryButton.gameObject.SetActive(shouldShowBattleParry);

        if (pauseButton != null)
            pauseButton.gameObject.SetActive(CanShowPauseButton(mode));
    }

    bool ShouldHideGameplayOverlay()
    {
        string scene = SceneManager.GetActiveScene().name;
        // Slideshow thuần (không có player movement) → ẩn toàn bộ mobile UI
        return scene == "Chapter0_Login" || scene == "Chapter1_Introduction" || scene == "Chapter1_CutScene" || scene == "Chapter4_WarNews" || scene == "Chapter6_Village" || scene == "Chapter6_Ending";
    }

    void BindButtons()
    {
        pauseButton?.onClick.AddListener(() => PauseMenuUI.Instance?.Toggle());
        battleBackButton?.onClick.AddListener(() => BattleManager.Instance?.BackToActionMenu());
        battleConfirmButton?.onClick.AddListener(() => BattleManager.Instance?.ConfirmAction());
        battleParryButton?.onClick.AddListener(() => BattleManager.Instance?.RequestParryMobile());

        // Map
        interactButton?.onClick.AddListener(() =>
            InputController.Instance?.QueueMobileInteract());
    }

    void CacheMovementTargets()
    {
        if (playerMovement == null)
            playerMovement = Object.FindFirstObjectByType<PlayerMovement>();

        if (cutsceneMovement == null)
            cutsceneMovement = Object.FindFirstObjectByType<PlayerMovement_Cutscene>();
    }

    void SetMobileMovement(Vector2 input)
    {
        playerMovement?.SetMobileInput(input);
        cutsceneMovement?.SetMobileInput(input);
    }

    bool CanUseJoystick()
    {
        // Retry khi chưa cache (ví dụ: test trực tiếp từ scene không có mode transition).
        if (playerMovement == null && cutsceneMovement == null)
            CacheMovementTargets();

        if (playerMovement != null)
            return true;

        if (cutsceneMovement != null)
            return cutsceneMovement.canMove;

        return false;
    }

    void EnsureBattleBackButton()
    {
        if (!autoCreateBattleBackButton || battleBackButton != null)
            return;

        battleBackButton = CreateRuntimeButton("BattleBackButtonRuntime", "Back", new Color(0.22f, 0.12f, 0.12f, 0.88f));
    }

    void EnsureBattleShortcutButtons()
    {
        if (!autoCreateBattleShortcutButtons)
            return;

        if (battleConfirmButton == null)
            battleConfirmButton = CreateRuntimeButton("BattleConfirmButtonRuntime", "OK", new Color(0.12f, 0.2f, 0.12f, 0.88f));

        if (battleParryButton == null)
            battleParryButton = CreateRuntimeButton("BattleParryButtonRuntime", "Parry", new Color(0.18f, 0.14f, 0.24f, 0.88f));
    }

    Button CreateRuntimeButton(string objectName, string label, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        createdRuntimeButton = true;

        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);

        var image = go.GetComponent<Image>();
        image.color = color;

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 30;
        text.color = Color.white;

        return go.GetComponent<Button>();
    }

    void ApplyMobileButtonLayout()
    {
        LayoutButton(interactButton, interactAnchor, buttonSize);
        LayoutButton(battleBackButton, backAnchor, buttonSize);
        LayoutButton(battleConfirmButton, confirmAnchor, buttonSize);
        LayoutBottomRight(battleParryButton, buttonSize); // Parry: khoá góc dưới-phải
    }

    /// <summary>Ghim nút vào GÓC DƯỚI-PHẢI màn hình, luôn nằm trọn (pivot góc phải-dưới + lề).</summary>
    void LayoutBottomRight(Button button, Vector2 size)
    {
        if (button == null) return;
        var rt = button.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); // neo vào góc dưới-phải màn hình
        rt.pivot = new Vector2(1f, 0f);                    // ghim góc phải-dưới của nút vào đó
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(-24f, 24f);      // chừa lề 24px cho khỏi sát mép
    }

    void LayoutButton(Button button, Vector2 anchor, Vector2 size)
    {
        if (button == null) return;

        var rt = button.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
    }

    void DisableLegacyBattleOverlay()
    {
        // Cách A: Battle dùng UI trong Chapter5a_Battle, không dùng overlay battle của prefab mobile.
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
