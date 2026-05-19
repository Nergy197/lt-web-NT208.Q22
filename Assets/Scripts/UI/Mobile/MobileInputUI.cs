using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay mobile: joystick di chuyển + các nút battle.
///
/// SETUP trong Unity:
///   1. Tạo Canvas (Screen Space – Overlay, Scale with Screen Size 1080×1920).
///   2. Gắn script này vào root GameObject của Canvas.
///   3. Tạo 3 Panel con: MapPanel, BattlePanel, SkillMenuPanel.
///   4. Trong MapPanel: thêm VirtualJoystick (bottom-left) + Button "Tương tác" (bottom-right).
///   5. Trong BattlePanel: Attack, SkillMenu, Parry, Flee, ← →, Confirm, Cancel.
///   6. Trong SkillMenuPanel: Skill1, Skill2, Skill3, Cancel.
///   7. Kéo các references vào Inspector.
/// </summary>
public class MobileInputUI : MonoBehaviour
{
    public static MobileInputUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject skillMenuPanel;

    [Header("Map – Joystick & Interact")]
    [SerializeField] private VirtualJoystick joystick;
    [SerializeField] private Button interactButton;

    [Header("Battle – Action Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillMenuButton;
    [SerializeField] private Button parryButton;
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button nextTargetButton;
    [SerializeField] private Button prevTargetButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelBattleButton;

    [Header("Skill Menu Buttons")]
    [SerializeField] private Button skill1Button;
    [SerializeField] private Button skill2Button;
    [SerializeField] private Button skill3Button;
    [SerializeField] private Button skillCancelButton;

    [Header("Settings")]
    [Tooltip("Bật để test giao diện mobile ngay trong Editor")]
    [SerializeField] private bool forceEnableInEditor = false;

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
        bool isMap    = mode == InputMode.Map || mode == InputMode.Cutscene;
        bool isBattle = mode == InputMode.Battle || mode == InputMode.BattleItemMenu;
        bool isSkill  = mode == InputMode.BattleSkillMenu;

        if (mapPanel)       mapPanel.SetActive(isMap);
        if (battlePanel)    battlePanel.SetActive(isBattle);
        if (skillMenuPanel) skillMenuPanel.SetActive(isSkill);
    }

    void BindButtons()
    {
        // Map
        interactButton?.onClick.AddListener(() =>
            InputController.Instance?.QueueMobileInteract());

        // Battle
        attackButton?.onClick.AddListener(()    => BattleManager.Instance?.SelectBasicAttack());
        skillMenuButton?.onClick.AddListener(() => BattleManager.Instance?.RequestOpenSkillMenu());
        parryButton?.onClick.AddListener(()     => BattleManager.Instance?.RequestParry());
        fleeButton?.onClick.AddListener(()      => BattleManager.Instance?.TryFlee());
        nextTargetButton?.onClick.AddListener(() => BattleManager.Instance?.ChangeTargetInput(1));
        prevTargetButton?.onClick.AddListener(() => BattleManager.Instance?.ChangeTargetInput(-1));
        confirmButton?.onClick.AddListener(()   => BattleManager.Instance?.ConfirmAction());
        cancelBattleButton?.onClick.AddListener(() => BattleManager.Instance?.BackToActionMenu());

        // Skill Menu
        skill1Button?.onClick.AddListener(()    => BattleManager.Instance?.UseSkill(0));
        skill2Button?.onClick.AddListener(()    => BattleManager.Instance?.UseSkill(1));
        skill3Button?.onClick.AddListener(()    => BattleManager.Instance?.UseSkill(2));
        skillCancelButton?.onClick.AddListener(() => BattleManager.Instance?.BackToActionMenu());
    }
}
