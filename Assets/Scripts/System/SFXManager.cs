using UnityEngine;

/// <summary>
/// Singleton SFX Manager — tồn tại xuyên suốt mọi scene.
/// Gán AudioClip trong Inspector, gọi SFXManager.Instance.Play___() từ bất kỳ đâu.
///
/// Thiết kế tối giản: chỉ dùng 1 AudioSource pool nhỏ cho PlayOneShot,
/// không có hệ thống phức tạp. Bạn tự chọn file âm thanh cho từng slot.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    // =====================================================================
    //  AUDIO CLIPS — Kéo file âm thanh vào Inspector
    // =====================================================================

    [Header("UI")]
    [Tooltip("Âm thanh khi ấn button bất kỳ")]
    public AudioClip buttonClick;

    [Tooltip("Âm thanh khi mở/đóng panel (menu, pause, save slots...)")]
    public AudioClip panelOpen;

    [Tooltip("Âm thanh khi đóng panel")]
    public AudioClip panelClose;

    [Header("Battle")]
    [Tooltip("Âm thanh khi trận đấu bắt đầu")]
    public AudioClip battleStart;

    [Tooltip("Âm thanh khi thắng trận")]
    public AudioClip battleWin;

    [Tooltip("Âm thanh khi thua trận")]
    public AudioClip battleLose;

    [Tooltip("Âm thanh khi bỏ chạy")]
    public AudioClip battleFlee;

    [Header("Unit")]
    [Tooltip("Âm thanh khi unit chết")]
    public AudioClip unitDied;

    [Tooltip("Âm thanh khi lên cấp")]
    public AudioClip levelUp;

    [Header("Map / Save")]
    [Tooltip("Âm thanh khi lưu game thành công")]
    public AudioClip saveSuccess;

    [Tooltip("Âm thanh khi chạm vào encounter trên map")]
    public AudioClip encounterTrigger;

    // =====================================================================
    //  VOLUME
    // =====================================================================

    [Header("Settings")]
    [Range(0f, 1f)]
    [Tooltip("Âm lượng chung cho tất cả SFX")]
    public float masterVolume = 1f;

    // =====================================================================
    //  INTERNALS
    // =====================================================================

    private AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // Tạo AudioSource nếu chưa có
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D sound
    }

    void OnEnable()
    {
        // Subscribe vào EventManager để tự động phát SFX
        EventManager.Subscribe(GameEvent.BattleStart,        OnBattleStart);
        EventManager.Subscribe(GameEvent.BattleWin,          OnBattleWin);
        EventManager.Subscribe(GameEvent.BattleLose,         OnBattleLose);
        EventManager.Subscribe(GameEvent.BattleFlee,         OnBattleFlee);
        EventManager.Subscribe(GameEvent.UnitDied,           OnUnitDied);
        EventManager.Subscribe(GameEvent.UnitLevelUp,        OnLevelUp);
        EventManager.Subscribe(GameEvent.EncounterTriggered, OnEncounter);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe(GameEvent.BattleStart,        OnBattleStart);
        EventManager.Unsubscribe(GameEvent.BattleWin,          OnBattleWin);
        EventManager.Unsubscribe(GameEvent.BattleLose,         OnBattleLose);
        EventManager.Unsubscribe(GameEvent.BattleFlee,         OnBattleFlee);
        EventManager.Unsubscribe(GameEvent.UnitDied,           OnUnitDied);
        EventManager.Unsubscribe(GameEvent.UnitLevelUp,        OnLevelUp);
        EventManager.Unsubscribe(GameEvent.EncounterTriggered, OnEncounter);
    }

    // =====================================================================
    //  PUBLIC API — Gọi từ bất kỳ script nào
    // =====================================================================

    /// <summary>Phát âm thanh button click.</summary>
    public void PlayButtonClick()   => Play(buttonClick);

    /// <summary>Phát âm thanh mở panel.</summary>
    public void PlayPanelOpen()     => Play(panelOpen);

    /// <summary>Phát âm thanh đóng panel.</summary>
    public void PlayPanelClose()    => Play(panelClose);

    /// <summary>Phát âm thanh lưu game thành công.</summary>
    public void PlaySaveSuccess()   => Play(saveSuccess);

    /// <summary>Phát một AudioClip bất kỳ (dùng cho trường hợp đặc biệt).</summary>
    public void PlayClip(AudioClip clip) => Play(clip);

    /// <summary>Phát clip với volume tuỳ chỉnh (0–1).</summary>
    public void PlayClip(AudioClip clip, float volumeScale) => Play(clip, volumeScale);

    // =====================================================================
    //  EVENT HANDLERS (tự động từ EventManager)
    // =====================================================================

    void OnBattleStart(object _) => Play(battleStart);
    void OnBattleWin(object _)   => Play(battleWin);
    void OnBattleLose(object _)  => Play(battleLose);
    void OnBattleFlee(object _)  => Play(battleFlee);
    void OnUnitDied(object _)    => Play(unitDied);
    void OnLevelUp(object _)     => Play(levelUp);
    void OnEncounter(object _)   => Play(encounterTrigger);

    // =====================================================================
    //  CORE PLAY
    // =====================================================================

    void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, masterVolume * volumeScale);
    }
}
