using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BattleUI: Giao dien chien dau hoan chinh.
/// Hien thi HP/AP bars, menu hanh dong, damage popup, turn order, status effect icons.
///
/// Cach su dung:
/// 1. Tao Canvas trong Battle Scene.
/// 2. Attach BattleUI vao Canvas.
/// 3. Gan cac tham chieu trong Inspector (xem Header sections).
/// 4. BattleUI tu dong cap nhat khi BattleManager chay.
/// </summary>
public class BattleUI : MonoBehaviour
{
    public static BattleUI Instance { get; private set; }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    // ================= PLAYER HUD =================

    [Header("Player HUD (gan cho moi player slot)")]
    [SerializeField] private List<UnitHUD> playerHUDs = new List<UnitHUD>();

    [Header("Enemy HUD (gan cho moi enemy slot)")]
    [SerializeField] private List<UnitHUD> enemyHUDs = new List<UnitHUD>();

    // ================= ACTION MENU =================

    [Header("Action Menu")]
    [SerializeField] private GameObject actionMenuPanel;
    [SerializeField] private Button btnAttack;
    [SerializeField] private Button btnSkill;
    [SerializeField] private Button btnFlee;
    [SerializeField] private Button btnParry;

    // ================= SKILL MENU =================

    [Header("Skill Menu")]
    [SerializeField] private GameObject skillMenuPanel;
    [SerializeField] private Button btnSkillBack;
    [SerializeField] private List<Button> skillButtons = new List<Button>();
    [SerializeField] private List<TextMeshProUGUI> skillLabels = new List<TextMeshProUGUI>();

    // ================= DAMAGE POPUP =================

    [Header("Damage Popup")]
    [SerializeField] private GameObject damagePopupPrefab;

    // ================= TURN ORDER =================

    [Header("Turn Order")]
    [SerializeField] private GameObject turnOrderPanel;
    [SerializeField] private TextMeshProUGUI turnOrderText;

    [Header("Turn Indicator")]
    [SerializeField] private TextMeshProUGUI turnIndicatorText;

    // ================= BATTLE LOG =================

    [Header("Battle Log")]
    [SerializeField] private TextMeshProUGUI battleLogText;
    private const int MAX_LOG_LINES = 8;
    private readonly List<string> logLines = new List<string>();

    // ================= STATUS EFFECT DISPLAY =================

    [Header("Status Effects")]
    [SerializeField] private TextMeshProUGUI playerEffectsText;
    [SerializeField] private TextMeshProUGUI enemyEffectsText;

    // ================= RESULT PANEL =================

    [Header("Battle Result")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI expText;

    // ================= INTERNAL =================

    private BattleManager bm;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Reset HUD ngay từ Awake để xoá các placeholder cố định trong scene
        // (ví dụ tên "Trần Quốc Tuấn" baked vào TMP) trước khi runtime data sẵn sàng.
        ClearAllHUDs();
    }

    void Start()
    {
        // An cac panel khi bat dau
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (turnIndicatorText != null) turnIndicatorText.text = string.Empty;
        if (turnOrderText != null) turnOrderText.text = string.Empty;
        if (battleLogText != null) battleLogText.text = string.Empty;
        if (playerEffectsText != null) playerEffectsText.text = string.Empty;
        if (enemyEffectsText != null) enemyEffectsText.text = string.Empty;

        // Bind buttons
        if (btnAttack != null) btnAttack.onClick.AddListener(OnAttackPressed);
        if (btnSkill != null) btnSkill.onClick.AddListener(OnSkillMenuOpen);
        if (btnFlee != null) btnFlee.onClick.AddListener(OnFleePressed);
        if (btnParry != null) btnParry.onClick.AddListener(OnParryPressed);
        if (btnSkillBack != null) btnSkillBack.onClick.AddListener(OnSkillMenuClose);

        // Bind skill buttons
        for (int i = 0; i < skillButtons.Count; i++)
        {
            int index = i; // capture for closure
            if (skillButtons[i] != null)
                skillButtons[i].onClick.AddListener(() => OnSkillSelected(index));
        }

        // Subscribe to events
        EventManager.Subscribe(GameEvent.BattleStart, OnBattleStart);
        EventManager.Subscribe(GameEvent.BattleWin, OnBattleWin);
        EventManager.Subscribe(GameEvent.BattleLose, OnBattleLose);
        EventManager.Subscribe(GameEvent.BattleFlee, OnBattleFlee);
        EventManager.Subscribe(GameEvent.UnitDied, OnUnitDied);

        StartCoroutine(WaitForBattleManager());
    }

    void OnDestroy()
    {
        EventManager.Unsubscribe(GameEvent.BattleStart, OnBattleStart);
        EventManager.Unsubscribe(GameEvent.BattleWin, OnBattleWin);
        EventManager.Unsubscribe(GameEvent.BattleLose, OnBattleLose);
        EventManager.Unsubscribe(GameEvent.BattleFlee, OnBattleFlee);
        EventManager.Unsubscribe(GameEvent.UnitDied, OnUnitDied);

        if (Instance == this) Instance = null;
    }

    IEnumerator WaitForBattleManager()
    {
        yield return new WaitUntil(() => BattleManager.Instance != null);
        bm = BattleManager.Instance;
    }

    void Update()
    {
        if (bm == null) return;

        // Chỉ update HUD khi cả hai party đã được khởi tạo. Nếu chưa, giữ
        // khung HUD ở trạng thái rỗng (đã reset trong Awake) để tránh nhấp nháy.
        if (bm.PlayerParty == null || bm.EnemyParty == null) return;

        UpdateAllHUDs();
        UpdateStatusEffects();
    }

    /// <summary>
    /// Đặt toàn bộ HUD player/enemy về trạng thái rỗng (giữ khung, không có dữ liệu).
    /// Gọi khi mới mở scene hoặc khi battle reset.
    /// </summary>
    public void ClearAllHUDs()
    {
        for (int i = 0; i < playerHUDs.Count; i++)
        {
            if (playerHUDs[i] == null) continue;
            playerHUDs[i].gameObject.SetActive(true);
            playerHUDs[i].SetEmpty();
        }
        for (int i = 0; i < enemyHUDs.Count; i++)
        {
            if (enemyHUDs[i] == null) continue;
            enemyHUDs[i].gameObject.SetActive(true);
            enemyHUDs[i].SetEmpty();
        }
    }

    // ================= HUD UPDATE =================

    void UpdateAllHUDs()
    {
        if (bm.PlayerParty != null)
        {
            for (int i = 0; i < playerHUDs.Count; i++)
            {
                if (playerHUDs[i] == null) continue;

                if (i < bm.PlayerParty.Members.Count)
                {
                    var member = bm.PlayerParty.Members[i];
                    var ps = member as PlayerStatus;
                    playerHUDs[i].gameObject.SetActive(true);
                    playerHUDs[i].UpdateHUD(member.entityName, member.currentHP, member.MaxHP,
                        ps != null ? ps.currentAP : -1, ps != null ? ps.MaxAP : -1);
                }
                else
                {
                    playerHUDs[i].gameObject.SetActive(false);
                }
            }
        }

        // Update Enemy HUDs
        if (bm.EnemyParty != null)
        {
            for (int i = 0; i < enemyHUDs.Count; i++)
            {
                if (enemyHUDs[i] == null) continue;

                if (i < bm.EnemyParty.Members.Count)
                {
                    var member = bm.EnemyParty.Members[i];
                    enemyHUDs[i].gameObject.SetActive(member.IsAlive);
                    if (member.IsAlive)
                        enemyHUDs[i].UpdateHUD(member.entityName, member.currentHP, member.MaxHP);
                }
                else
                {
                    enemyHUDs[i].gameObject.SetActive(false);
                }
            }
        }
    }

    void UpdateStatusEffects()
    {
        if (playerEffectsText != null && bm.PlayerParty != null)
        {
            string text = "";
            foreach (var member in bm.PlayerParty.Members)
            {
                if (!member.IsAlive) continue;
                var effects = member.GetActiveEffects();
                if (effects.Count > 0)
                {
                    text += $"{member.entityName}: ";
                    foreach (var e in effects)
                        text += $"[{e.effectName} {e.duration}t] ";
                    text += "\n";
                }
            }
            playerEffectsText.text = text;
        }
    }

    // ================= ACTION MENU =================

    /// <summary>
    /// Goi boi BattleManager khi den luot player.
    /// </summary>
    public void ShowActionMenu(PlayerStatus player)
    {
        if (actionMenuPanel != null) actionMenuPanel.SetActive(true);
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);

        // Update turn indicator + highlight active HUD slot
        if (player != null)
        {
            SetTurnIndicator($"Luot: {player.entityName}");
            HighlightActivePlayerHUD(player.BattleSlotId);
        }

        // Update skill menu content
        UpdateSkillMenu(player);
    }

    public void HideActionMenu()
    {
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Highlight player HUD dang den luot, tat highlight cac slot khac.
    /// Goi -1 de tat tat ca highlight (vi du khi den luot enemy).
    /// </summary>
    public void HighlightActivePlayerHUD(int slotId)
    {
        for (int i = 0; i < playerHUDs.Count; i++)
        {
            if (playerHUDs[i] != null)
                playerHUDs[i].SetHighlight(i == slotId);
        }
    }

    /// <summary>
    /// Cap nhat text Turn Indicator (top-center).
    /// </summary>
    public void SetTurnIndicator(string label)
    {
        if (turnIndicatorText != null) turnIndicatorText.text = label;
    }

    void UpdateSkillMenu(PlayerStatus player)
    {
        if (player == null) return;

        for (int i = 0; i < skillButtons.Count; i++)
        {
            if (skillButtons[i] == null) continue;

            if (i < player.SkillCount)
            {
                var skill = player.GetSkillByIndex(i);
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].interactable = player.CanUseAP(skill.apCost);

                if (i < skillLabels.Count && skillLabels[i] != null)
                    skillLabels[i].text = $"{skill.attackName} (AP: {skill.apCost})";
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ================= BUTTON HANDLERS =================

    void OnAttackPressed()
    {
        if (bm == null) return;
        HideActionMenu();
        bm.SelectBasicAttack();
    }

    void OnSkillMenuOpen()
    {
        if (actionMenuPanel != null) actionMenuPanel.SetActive(false);
        if (skillMenuPanel != null) skillMenuPanel.SetActive(true);
    }

    void OnSkillMenuClose()
    {
        if (skillMenuPanel != null) skillMenuPanel.SetActive(false);
        if (actionMenuPanel != null) actionMenuPanel.SetActive(true);
    }

    void OnSkillSelected(int index)
    {
        if (bm == null) return;
        HideActionMenu();
        bm.UseSkill(index);
    }

    void OnFleePressed()
    {
        if (bm == null) return;
        HideActionMenu();
        bm.TryFlee();
    }

    void OnParryPressed()
    {
        if (bm == null) return;
        bm.RequestParry();
    }

    // ================= DAMAGE POPUP =================

    /// <summary>
    /// Hien thi damage popup tai vi tri unit.
    /// Goi tu ben ngoai: BattleUI.Instance.ShowDamagePopup(position, damage, isHeal)
    /// </summary>
    public void ShowDamagePopup(Vector3 worldPos, int value, bool isHeal = false)
    {
        if (damagePopupPrefab == null) return;

        var popup = Instantiate(damagePopupPrefab, worldPos + Vector3.up * 0.5f, Quaternion.identity);
        TMP_Text tmp = popup.GetComponentInChildren<TMP_Text>();

        if (tmp != null)
        {
            tmp.text = isHeal ? $"+{value}" : $"-{value}";
            tmp.color = isHeal ? Color.green : Color.red;
        }

        // Tu dong destroy sau 1 giay
        Destroy(popup, 1f);
    }

    // ================= BATTLE LOG =================

    public void Log(string message)
    {
        logLines.Add(message);
        while (logLines.Count > MAX_LOG_LINES)
            logLines.RemoveAt(0);

        if (battleLogText != null)
            battleLogText.text = string.Join("\n", logLines);
    }

    // ================= TURN ORDER DISPLAY =================

    public void UpdateTurnOrder(string info)
    {
        if (turnOrderText != null)
            turnOrderText.text = info;
    }

    // ================= EVENT HANDLERS =================

    void OnBattleStart(object data)
    {
        Log("Battle Start!");
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void OnBattleWin(object data)
    {
        ShowResult("THANG!", true);
    }

    void OnBattleLose(object data)
    {
        ShowResult("THUA!", false);
    }

    void OnBattleFlee(object data)
    {
        ShowResult("DA BO CHAY!", false);
    }

    void OnUnitDied(object data)
    {
        var unit = data as Status;
        if (unit != null)
            Log($"{unit.entityName} da bi ha guc!");
    }

    void ShowResult(string result, bool isWin)
    {
        HideActionMenu();
        HighlightActivePlayerHUD(-1);
        SetTurnIndicator(string.Empty);
        if (resultPanel != null) resultPanel.SetActive(true);
        if (resultText != null)
        {
            resultText.text = result;
            resultText.color = isWin ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.4f);
        }
        if (expText != null) expText.text = string.Empty;
    }

    public void SetExpResult(string expInfo)
    {
        if (expText != null) expText.text = expInfo;
    }
}
