using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    /// <summary>Expose để PlayerAttack.ResolveEffectTarget có thể lấy ally target.</summary>
    public Party PlayerParty => playerParty;

    /// <summary>Expose để BattleUI có thể update enemy HUDs.</summary>
    public Party EnemyParty => enemyParty;

    private Party playerParty;
    private Party enemyParty;

    private TurnManager turnManager;
    private Status currentUnit;

    private bool waitingForPlayerAction = false;
    private bool waitingForAttackFinish = false;

    private bool playerWon = false;
    private bool playerFled = false;

    private int currentTargetIndex = 0;
    private int currentAllyTargetIndex = 0;

    [Header("Spawn Positions")]
    [SerializeField] private Transform playerSpawnAnchor;
    [SerializeField] private Transform enemySpawnAnchor;
    [SerializeField] private float spawnSpacing = 2f;
    [Tooltip("Scale nhân vật player khi spawn (1 = giữ nguyên prefab).")]
    [SerializeField] private float playerSpawnScale = 1f;
    [Tooltip("Scale kẻ địch khi spawn — chỉnh để bằng cỡ player.")]
    [SerializeField] private float enemySpawnScale = 1f;

    [Header("Camera")]
    [Tooltip("Orthographic size của camera khi vào battle (tăng để thu xa, giảm để gần).")]
    [SerializeField] private float battleCameraSize = 4f;

    [Header("UI System")]
    [SerializeField] private GameObject targetCursor;
    [SerializeField] private Vector3 cursorOffset = new Vector3(0, 0f, 0);
    [Tooltip("Cursor chỉ vào player đang hành động.")]
    [SerializeField] private GameObject playerTurnCursor;
    [SerializeField] private Vector3 playerCursorOffset = new Vector3(-1.2f, 0f, 0f);

    [Header("Enemy World HUD")]
    [Tooltip("Prefab WorldSpace canvas có UnitHUD — spawn dưới chân mỗi enemy lúc battle bắt đầu.")]
    [SerializeField] private GameObject enemyHUDPrefab;
    [Tooltip("Offset local so với model enemy — âm Y để nằm dưới chân sprite.")]
    [SerializeField] private Vector3 enemyHUDOffset = new Vector3(0f, -1f, 0f);
    [Tooltip("Scale của canvas HP enemy sau khi bù parent scale (0.5–1 là vừa).")]
    [SerializeField] private float enemyHUDCanvasScale = 0.7f;

    [Header("Demo/Test Mode")]
    [SerializeField] private bool useDemoModeIfMissingManager = true;
    [Tooltip("Kéo PlayerData asset vào để test — tự tạo player với đủ stats, prefab, skills.")]
    [SerializeField] private PlayerData debugPlayerData;
    [Tooltip("Fallback nếu không có debugPlayerData và không có MapData enemy.")]
    [SerializeField] private GameObject debugEnemyPrefab;
    [SerializeField] private EnemyAttackData debugEnemyAttack;

    private bool isTargetingAlly = false;

    void Log(string msg)
    {
        Debug.Log(msg);
        if (BattleDebugUI.Instance != null)
            BattleDebugUI.Instance.Log(msg);
    }

    IEnumerator Start()
    {
        Instance = this;
        Log("[BATTLE] Start");

        BattleEvents.OnAttackFinished += OnAttackFinished;

        // Đợi GameManager sẵn sàng. Bỏ qua nếu demo mode đã bật.
        if (!useDemoModeIfMissingManager)
        {
            float timeout = 5f;
            while ((GameManager.Instance == null || !GameManager.Instance.isLoaded) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            // Demo mode: chờ 1 frame để các singleton khác kịp Awake
            yield return null;
        }

        if (GameManager.Instance == null || !GameManager.Instance.isLoaded)
        {
            if (useDemoModeIfMissingManager)
            {
                Log("[BATTLE] GameManager chưa sẵn sàng — Kích hoạt DEMO MODE.");
                LoadDemoData();
            }
            else
            {
                Debug.LogError("[BATTLE] GameManager chưa sẵn sàng — không thể vào trận. Quay lại MapScene.");
                FailSafeBackToMap();
                yield break;
            }
        }
        else
        {
            playerParty = GameManager.Instance.GetPlayerParty();
            
            if (playerParty == null || playerParty.Members == null || playerParty.Members.Count == 0)
            {
                Log("[BATTLE] Player party rỗng — Kích hoạt DEMO MODE.");
                LoadDemoData();
            }
            else
            {
                if (MapManager.Instance == null)
                {
                    Log("[BATTLE] MapManager null — Kích hoạt DEMO ENEMY.");
                    LoadDemoEnemy();
                }
                else
                {
                    LoadEnemy();
                    if (enemyParty == null || enemyParty.Members.Count == 0) LoadDemoEnemy();
                }
            }
        }

        // Guard 4: InputController không bắt buộc cho battle loop, nhưng cảnh báo
        if (InputController.Instance != null)
            InputController.Instance.BindBattleManager(this);

        InitBattle();
    }

    /// <summary>
    /// Quay về MapScene khi battle không thể bắt đầu hợp lệ. Reset cờ
    /// MapManager.isInBattle để random encounter tiếp tục hoạt động.
    /// </summary>
    void FailSafeBackToMap()
    {
        if (MapManager.Instance != null)
        {
            MapManager.Instance.isInBattle = false;
            if (MapManager.Instance.currentEnemies != null)
                MapManager.Instance.currentEnemies.Clear();
        }
        if (InputController.Instance != null)
            InputController.Instance.UnbindBattleManager();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    void OnDestroy()
    {
        BattleEvents.OnAttackFinished -= OnAttackFinished;
        if (Instance == this) Instance = null;
    }

    void OnAttackFinished()
    {
        waitingForAttackFinish = false;
        Log("[ATTACK] Finished");
        
        var dialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
        if (dialog != null)
        {
            var p = GetAlivePlayer();
            var e = GetEnemyTarget();
            if (p != null) dialog.UpdateBuffDebuff(p);
            if (e != null) dialog.UpdateEnemyCombo(e.PlannedAttack);
        }
    }

    void Update()
    {
        if (targetCursor == null) return;

        if (!waitingForPlayerAction)
        {
            if (targetCursor.activeSelf) targetCursor.SetActive(false);
            return;
        }

        Status currentSelectedTarget = isTargetingAlly ? GetAllyTarget() : GetEnemyTarget();

        if (currentSelectedTarget != null && currentSelectedTarget.SpawnedModel != null)
        {
            if (!targetCursor.activeSelf) targetCursor.SetActive(true);
            var tv = currentSelectedTarget.SpawnedModel.GetComponent<UnitVisual>();
            Vector3 center = tv != null ? tv.SpriteCenter : currentSelectedTarget.SpawnedModel.transform.position;
            // cursorOffset.x là khoảng cách trước mặt — tự đổi dấu theo hướng sprite
            float facing = tv != null ? tv.FacingSign : 1f;
            Vector3 offset = new Vector3(cursorOffset.x * facing, cursorOffset.y, cursorOffset.z);
            targetCursor.transform.position = center + offset;
        }
        else
        {
            if (targetCursor.activeSelf) targetCursor.SetActive(false);
        }

        // Cursor chỉ vào player đang hành động
        if (playerTurnCursor != null)
        {
            if (waitingForPlayerAction && currentUnit != null && currentUnit.SpawnedModel != null)
            {
                playerTurnCursor.SetActive(true);
                var pv = currentUnit.SpawnedModel.GetComponent<UnitVisual>();
                Vector3 center = pv != null ? pv.SpriteCenter : currentUnit.SpawnedModel.transform.position;
                float facing = pv != null ? pv.FacingSign : 1f;
                Vector3 offset = new Vector3(playerCursorOffset.x * facing, playerCursorOffset.y, playerCursorOffset.z);
                playerTurnCursor.transform.position = center + offset;
            }
            else
            {
                playerTurnCursor.SetActive(false);
            }
        }
    }

    UnitHUD SetupEnemyHUD(GameObject model, Status enemy)
    {
        // Tìm Canvas Screen Space
        Canvas uiCanvas = null;
        if (BattleUI.Instance != null)
        {
            uiCanvas = BattleUI.Instance.GetComponent<Canvas>();
            if (uiCanvas == null) uiCanvas = BattleUI.Instance.GetComponentInParent<Canvas>();
        }
        if (uiCanvas == null) uiCanvas = Object.FindFirstObjectByType<Canvas>();
        if (uiCanvas == null) return null;

        // Root bar — Screen Space, pivot center
        var root  = new GameObject("EnemyHPBar", typeof(RectTransform));
        root.transform.SetParent(uiCanvas.transform, false);
        var rt    = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 10f);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        // Background tối
        var bg = new GameObject("BG", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bg.transform.SetParent(root.transform, false);
        bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);
        FillRect(bg.GetComponent<RectTransform>());

        // Fill đỏ — dùng anchorMax.x để co dãn, không cần sprite hay fillAmount
        float initRatio = enemy.MaxHP > 0 ? (float)enemy.currentHP / enemy.MaxHP : 1f;
        var fillGO  = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        fillGO.transform.SetParent(root.transform, false);
        fillGO.GetComponent<UnityEngine.UI.Image>().color = new Color(0.88f, 0.06f, 0.06f);
        var fillRt       = fillGO.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(initRatio, 1f);
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);

        var bar          = root.AddComponent<EnemyHPBar>();
        bar.trackedEnemy = enemy;
        bar.fillRect     = fillRt;
        bar.worldOffset  = enemyHUDOffset;

        return null;
    }

    static void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void LoadEnemy()
    {
        enemyParty = new Party(PartyType.Enemy);
        if (MapManager.Instance == null) return;

        var statuses = MapManager.Instance.CreateEnemyStatuses();
        if (statuses == null) return;
        foreach (var e in statuses)
        {
            if (e == null) continue;
            enemyParty.AddMember(e);
            Log($"[BATTLE] Spawn enemy: {e.entityName} Lv{e.level}");
        }
    }

    void LoadDemoData()
    {
        playerParty = new Party(PartyType.Player);
        if (debugPlayerData != null)
        {
            playerParty.AddMember(debugPlayerData.CreateStatus());
        }
        else
        {
            var p = new PlayerStatus("Player Demo", 100, 20, 10, 15);
            playerParty.AddMember(p);
            Log("[BATTLE] debugPlayerData chưa gán — dùng player fallback không có prefab/skill.");
        }

        // Ưu tiên enemy từ MapData (nạp qua BattleSceneBootstrap)
        if (MapManager.Instance != null &&
            MapManager.Instance.currentEnemies != null &&
            MapManager.Instance.currentEnemies.Count > 0)
        {
            LoadEnemy();
        }
        else
        {
            LoadDemoEnemy();
        }
    }

    void LoadDemoEnemy()
    {
        enemyParty = new Party(PartyType.Enemy);
        var e1 = new EnemyStatus("Enemy Demo", 50, 15, 5, 10);
        e1.battlePrefab = debugEnemyPrefab;
        if (debugEnemyAttack != null) e1.AddAttack(debugEnemyAttack);
        enemyParty.AddMember(e1);
        Log("[BATTLE] debugEnemyPrefab fallback — gán debugMapData vào BattleSceneBootstrap để dùng enemy thật.");
    }

    void InitBattle()
    {
        // Zoom camera ra theo thiết lập
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
            cam.orthographicSize = battleCameraSize;

        // Tạo TurnManager mới cho trận đấu này
        turnManager = new TurnManager();
        turnManager.AddParty(playerParty);
        turnManager.AddParty(enemyParty);

        // Apply map effects cho player khi vào trận
        if (MapManager.Instance != null)
        {
            foreach (var p in playerParty.Members)
            {
                var ps = p as PlayerStatus;
                if (ps != null) MapManager.Instance.ApplyPlayerEffects(ps);
            }
        }

        SpawnParty(playerParty, playerSpawnAnchor, spawnSpacing);
        SpawnParty(enemyParty, enemySpawnAnchor, spawnSpacing);

        Log("[BATTLE] Init Complete");
        PlanEnemyActions(); // Lên kế hoạch ngay từ đầu trận

        var dialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
        if (dialog != null)
        {
            var p = GetAlivePlayer();
            var e = GetEnemyTarget();
            if (p != null) dialog.UpdateBuffDebuff(p);
            if (e != null) dialog.UpdateEnemyCombo(e.PlannedAttack);
        }

        EventManager.Publish(GameEvent.BattleStart);
        StartCoroutine(BattleLoop());
    }

    void SpawnParty(Party party, Transform anchor, float spacing)
    {
        if (anchor == null)
        {
            Log("[WARN] No spawn anchor for " + party.Type);
            return;
        }

        int count = party.Members.Count;

        // Căn giữa nhóm xung quanh anchor.
        // Ví dụ: 3 thành viên, spacing=2 → offsets: -2, 0, +2
        float totalWidth = (count - 1) * spacing;
        float startOffset = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            var member = party.Members[i];

            GameObject prefab = null;
            if (member is PlayerStatus ps) prefab = ps.battlePrefab;
            else if (member is EnemyStatus es) prefab = es.battlePrefab;

            if (prefab == null)
            {
                Log("[WARN] No battlePrefab for " + member.entityName);
                member.BattleSlotId = i;
                continue;
            }

            // Index 0 đứng đầu hàng (vị trí âm nhất trên trục Y)
            Vector3 offset = new Vector3(0, -(startOffset + i * spacing), 0);
            Vector3 spawnPos = anchor.position + offset;

            var model = Object.Instantiate(prefab, spawnPos, anchor.rotation);
            float scale = member is PlayerStatus ? playerSpawnScale : enemySpawnScale;
            if (scale != 1f) model.transform.localScale *= scale;
            member.SpawnedModel = model;
            member.BattleSlotId = i;

            // Enemy prefab đã có Enemy_HP_Canvas (giống Tutorial) — wire UnitHUD runtime
            if (member is EnemyStatus)
            {
                var hud = SetupEnemyHUD(model, member);
                if (hud != null)
                    BattleUI.Instance?.RegisterEnemyHUD(i, hud);
            }

            Log($"[SPAWN] {member.entityName} → {spawnPos}");
        }
    }

    IEnumerator BattleLoop()
    {
        // Đợi 1 frame để SpriteRenderer.bounds và các component khác được Unity khởi tạo sau spawn
        yield return null;

        while (true)
        {
            if (CheckEndBattle()) break;

            currentUnit = turnManager.GetNextUnit();
            if (currentUnit == null || !currentUnit.IsAlive) continue;

            Log("[TURN] " + currentUnit.entityName);

            if (BattleUI.Instance != null)
                BattleUI.Instance.UpdateTurnOrder(BuildTurnOrderInfo());

            // Xử lý Poison và Stun trước khi unit hành động (delegate sang TurnManager).
            bool isStunned = turnManager.ProcessTurnEffects(currentUnit);

            // Poison có thể kết thúc trận — kiểm tra ngay sau khi áp damage.
            if (CheckEndBattle()) break;

            if (isStunned)
            {
                Log($"[STUN] {currentUnit.entityName} mất lượt!");
                currentUnit.UpdateEffectDurations();
                continue;
            }

            // Skip turn nếu unit vừa bị Poison kill trong cùng frame.
            if (!currentUnit.IsAlive)
            {
                currentUnit.UpdateEffectDurations();
                continue;
            }

            if (currentUnit is PlayerStatus)
                yield return PlayerTurn();
            else
                yield return EnemyTurn();

            currentUnit.UpdateEffectDurations();
        }

        EndBattle();
    }

    public void PlanEnemyActions()
    {
        if (enemyParty == null || playerParty == null) return;
        foreach (var e in enemyParty.Members)
        {
            var enemy = e as EnemyStatus;
            if (enemy != null && enemy.IsAlive && enemy.PlannedAttack == null)
            {
                if (enemy.HasAI) enemy.ai.PlanAction(this, enemy, playerParty, enemyParty);
                else enemy.SetPlannedAction(GetAlivePlayer(), enemy.GetRandomAttack());
            }
        }
    }

    IEnumerator PlayerTurn()
    {
        waitingForPlayerAction = true;
        waitingForAttackFinish = false;
        Log("[TURN] Waiting Player Action");

        PlanEnemyActions(); // Ensure all enemies have a plan before player decides

        var playerUnit = currentUnit as PlayerStatus;
        if (BattleUI.Instance != null && playerUnit != null)
        {
            BattleUI.Instance.ShowActionMenu(playerUnit);
            BattleUI.Instance.HighlightEnemyHUD(currentTargetIndex);
            var target = GetEnemyTarget();
            if (target != null) BattleUI.Instance.SetTargetName(target.entityName);
            
            var dialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
            if (dialog != null)
            {
                dialog.UpdateBuffDebuff(playerUnit);
                if (target != null) dialog.UpdateEnemyCombo(target.PlannedAttack);
                else dialog.UpdateEnemyCombo(null);
            }
        }

        // Buoc 1: Doi player chon hanh dong (attack, skill, flee...).
        yield return new WaitUntil(() => !waitingForPlayerAction);

        // An action menu sau khi chon
        if (BattleUI.Instance != null)
        {
            BattleUI.Instance.HideActionMenu();
            BattleUI.Instance.HighlightEnemyHUD(-1);
            BattleUI.Instance.SetTargetName(string.Empty);
        }

        // Buoc 2: Doi coroutine attack chay den phase Finished.
        if (waitingForAttackFinish)
        {
            Log("[TURN] Waiting for attack to finish...");
            yield return new WaitUntil(() => !waitingForAttackFinish);
        }
    }

    // -1 = basicAttack, 0+ = skill index, -2 = chưa chọn
    private int pendingSkillIndex = -2;
    private bool isSelectingTarget = false;


    public void BackToActionMenu()
    {
        if (!waitingForPlayerAction) return;
        isSelectingTarget = false;
        pendingSkillIndex = -2;
        BattleUI.Instance?.HighlightEnemyHUD(-1);
        BattleUI.Instance?.SetTargetName(string.Empty);
        BattleUI.Instance?.ShowActionMenu(currentUnit as PlayerStatus);
        InputController.Instance?.SetMode(InputMode.Battle);
    }

    public void RequestOpenSkillMenu()
    {
        if (!waitingForPlayerAction || isSelectingTarget) return;
        var player = currentUnit as PlayerStatus;
        BattleUI.Instance?.ShowSkillMenuUI(player);
        InputController.Instance?.SetMode(InputMode.BattleSkillMenu);
    }

    void EnterTargetSelection(int skillIndex)
    {
        pendingSkillIndex = skillIndex;
        isSelectingTarget = true;
        BattleUI.Instance?.HideActionMenu();
        BattleUI.Instance?.HighlightEnemyHUD(currentTargetIndex);
        var t = GetEnemyTarget();
        if (t != null) 
        {
            BattleUI.Instance?.SetTargetName(t.entityName);
            var dialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
            if (dialog != null)
            {
                dialog.UpdateBuffDebuff(currentUnit);
                dialog.UpdateEnemyCombo(t.PlannedAttack);
            }
        }
        // Đảm bảo mode Battle để Confirm/Cancel hoạt động dù vào từ SkillMenu
        InputController.Instance?.SetMode(InputMode.Battle);
        Log("[TARGET] A/D: đổi mục tiêu | Enter: xác nhận | Esc: huỷ");
    }

    public void SelectBasicAttack()
    {
        if (!waitingForPlayerAction || isSelectingTarget) return;
        var player = currentUnit as PlayerStatus;
        if (player?.BasicAttack == null) { Log("[ERROR] No basic attack"); return; }
        EnterTargetSelection(-1);
    }

    public void UseSkill(int index)
    {
        if (!waitingForPlayerAction || isSelectingTarget) return;
        var player = currentUnit as PlayerStatus;
        if (player == null) return;
        var skill = player.GetSkillByIndex(index);
        if (skill == null) { Log($"[ERROR] Skill[{index}] not found"); return; }
        if (!player.CanUseAP(skill.apCost)) { Log($"[ERROR] Not enough AP"); return; }
        EnterTargetSelection(index);
    }

    public void ConfirmAction()
    {
        if (!waitingForPlayerAction || !isSelectingTarget) return;
        isSelectingTarget = false;

        var player = currentUnit as PlayerStatus;
        var enemy  = GetEnemyTarget();
        if (player == null || enemy == null) { Log("[ERROR] Invalid target"); return; }

        if (pendingSkillIndex == -1)
        {
            Log("[ACTION] Attack → " + enemy.entityName);
            waitingForAttackFinish = true;
            player.BasicAttack.CreateInstance().Use(player, enemy);
        }
        else
        {
            var skill = player.GetSkillByIndex(pendingSkillIndex);
            if (skill == null || !player.CanUseAP(skill.apCost)) return;
            Log($"[ACTION] Skill[{pendingSkillIndex}] → {skill.attackName}");
            waitingForAttackFinish = true;
            skill.CreateInstance().Use(player, enemy);
        }

        pendingSkillIndex = -2;
        waitingForPlayerAction = false;
    }

    public void CancelTargetSelection()
    {
        if (!isSelectingTarget) return;
        isSelectingTarget = false;
        pendingSkillIndex = -2;
        BattleUI.Instance?.HighlightEnemyHUD(-1);
        BattleUI.Instance?.SetTargetName(string.Empty);
        BattleUI.Instance?.ShowActionMenu(currentUnit as PlayerStatus);
    }

    public void RequestParry()
    {
        // Gửi yêu cầu parry đến tất cả player còn sống.
        // PlayerStatus sẽ từ chối nếu parryWindow chưa mở hoặc đang trong cooldown penalty.
        foreach (var p in playerParty.Members)
        {
            var ps = p as PlayerStatus;
            if (ps != null && ps.IsAlive)
                ps.RequestParry();
        }
        Log("[ACTION] Parry Requested");
    }

    public void TryFlee()
    {
        if (!waitingForPlayerAction) return;

        var player = currentUnit as PlayerStatus;
        if (player == null) return;

        Log("[ACTION] Try Flee");

        int maxEnemySpd = 1;
        foreach (var e in enemyParty.Members)
            if (e.IsAlive && e.Spd > maxEnemySpd) maxEnemySpd = e.Spd;

        // Ti le bo chay co ban 50%, dieu chinh +-5% cho moi diem chenh lech Speed.
        float chance = Mathf.Clamp(0.5f + (player.Spd - maxEnemySpd) * 0.05f, 0.1f, 0.95f);

        if (Random.value <= chance)
        {
            Log("[FLEE] Success!");
            playerFled = true;
        }
        else
        {
            Log("[FLEE] Failed! Turn wasted.");
        }

        waitingForPlayerAction = false;
    }

    /// <summary>
    /// Duoc goi boi EnemyAI de chi dinh target cu the thay vi random.
    /// </summary>
    public void SetEnemyTarget(PlayerStatus target)
    {
        if (target == null) return;
        // Tim index cua target trong danh sach alive players
        var alive = new List<PlayerStatus>();
        foreach (var p in playerParty.Members)
            if (p.IsAlive) alive.Add(p as PlayerStatus);
        
        for (int i = 0; i < alive.Count; i++)
        {
            if (alive[i] == target)
            {
                currentAllyTargetIndex = i;
                break;
            }
        }
    }

    IEnumerator EnemyTurn()
    {
        var enemy = currentUnit as EnemyStatus;

        // Guard: nếu currentUnit không phải EnemyStatus (lỗi dữ liệu), bỏ qua lượt.
        if (enemy == null)
        {
            Log("[ENEMY] currentUnit không phải EnemyStatus — bỏ qua lượt.");
            yield break;
        }

        // Tat highlight player + cap nhat turn indicator hien thi luot enemy
        if (BattleUI.Instance != null)
        {
            BattleUI.Instance.HighlightActivePlayerHUD(-1);
            BattleUI.Instance.HighlightEnemyHUD(-1);
            BattleUI.Instance.SetTargetName(string.Empty);
            BattleUI.Instance.SetTurnIndicator($"Luot dich: {enemy.entityName}");
        }

        // Neu chua co plan, lap tuc plan (truong hop enemy di truoc player turn)
        if (enemy.PlannedAttack == null || enemy.PlannedTarget == null || !enemy.PlannedTarget.IsAlive)
        {
            if (enemy.HasAI) enemy.ai.PlanAction(this, enemy, playerParty, enemyParty);
            else enemy.SetPlannedAction(GetAlivePlayer(), enemy.GetRandomAttack());
        }

        var attackData = enemy.PlannedAttack;
        var target = enemy.PlannedTarget;

        if (attackData == null || target == null || !target.IsAlive)
        {
            Log($"[ENEMY] {enemy.entityName} has no valid target or attack -- skipping turn.");
            enemy.SetPlannedAction(null, null); // Clear plan
            yield break;
        }

        Log("[ENEMY] Attack -> " + target.entityName);

        // Thông báo đòn đánh lên dialog trước khi execute
        BattleEvents.RaiseEnemyAttackAnnounced(attackData, enemy, target);

        // Set co truoc khi Use() vi parry window chay ben trong EnemyAttack.Execute().
        waitingForAttackFinish = true;
        SetEnemyTarget(target); // for consistency
        attackData.CreateInstance().Use(enemy, target);

        // Doi toan bo attack (wind-up + parry window + impact + recovery) hoan thanh.
        yield return new WaitUntil(() => !waitingForAttackFinish);
        
        enemy.SetPlannedAction(null, null); // Clear plan sau khi danh xong
    }

    EnemyStatus GetEnemyTarget()
    {
        if (enemyParty == null) return null;
        var alive = new List<EnemyStatus>();
        foreach (var e in enemyParty.Members)
            if (e.IsAlive) alive.Add(e as EnemyStatus);

        if (alive.Count == 0) return null;

        // Auto-clamp khi target vừa chết để không bao giờ trả về null khi còn địch.
        currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, alive.Count - 1);
        return alive[currentTargetIndex];
    }

    PlayerStatus GetAlivePlayer()
    {
        if (playerParty == null) return null;
        foreach (var p in playerParty.Members)
            if (p.IsAlive) return p as PlayerStatus;
        return null;
    }

    public PlayerStatus GetAllyTarget()
    {
        if (playerParty == null) return null;
        var alive = new List<PlayerStatus>();
        foreach (var p in playerParty.Members)
            if (p.IsAlive) alive.Add(p as PlayerStatus);

        if (alive.Count == 0) return null;

        currentAllyTargetIndex = Mathf.Clamp(currentAllyTargetIndex, 0, alive.Count - 1);
        return alive[currentAllyTargetIndex];
    }

    private float _lastTargetChangeTime = -1f;
    private const float TargetChangeCooldown = 0.2f;

    public void ChangeTargetInput(int dir)
    {
        if (enemyParty == null || playerParty == null) return;
        if (Time.unscaledTime - _lastTargetChangeTime < TargetChangeCooldown) return;
        _lastTargetChangeTime = Time.unscaledTime;

        var aliveEnemies = new List<EnemyStatus>();
        foreach (var e in enemyParty.Members)
            if (e.IsAlive) aliveEnemies.Add(e as EnemyStatus);

        var aliveAllies = new List<PlayerStatus>();
        foreach (var p in playerParty.Members)
            if (p.IsAlive) aliveAllies.Add(p as PlayerStatus);

        // Nếu đang target ally (heal/buff), cycle qua danh sách ally.
        // Mặc định cycle qua danh sách địch.
        if (!isTargetingAlly && aliveEnemies.Count > 0)
        {
            currentTargetIndex = (currentTargetIndex + dir + aliveEnemies.Count) % aliveEnemies.Count;
            Log("[TARGET ENEMY] → " + aliveEnemies[currentTargetIndex].entityName);
            
            if (BattleUI.Instance != null)
            {
                BattleUI.Instance.HighlightEnemyHUD(currentTargetIndex);
                BattleUI.Instance.SetTargetName(aliveEnemies[currentTargetIndex].entityName);
            }
            
            var dialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
            if (dialog != null)
            {
                dialog.UpdateBuffDebuff(currentUnit);
                dialog.UpdateEnemyCombo(aliveEnemies[currentTargetIndex].PlannedAttack);
            }
        }
        else if (isTargetingAlly && aliveAllies.Count > 0)
        {
            currentAllyTargetIndex = (currentAllyTargetIndex + dir + aliveAllies.Count) % aliveAllies.Count;
            Log("[TARGET ALLY] → " + aliveAllies[currentAllyTargetIndex].entityName);
            
            if (BattleUI.Instance != null)
                BattleUI.Instance.SetTargetName(aliveAllies[currentAllyTargetIndex].entityName);
                
            var dialog = Object.FindFirstObjectByType<BattleInfoDialogUI>();
            if (dialog != null)
            {
                dialog.UpdateBuffDebuff(aliveAllies[currentAllyTargetIndex]);
                dialog.UpdateEnemyCombo(null); // Allies don't have PlannedAttack to show here
            }
        }
    }

    string BuildTurnOrderInfo()
    {
        // Tom luoc party con song de hien thi tren UI (turn order panel).
        int playerAlive = 0, enemyAlive = 0;
        foreach (var p in playerParty.Members) if (p.IsAlive) playerAlive++;
        foreach (var e in enemyParty.Members) if (e.IsAlive) enemyAlive++;
        return $"Player: {playerAlive}  |  Enemy: {enemyAlive}";
    }

    bool CheckEndBattle()
    {
        if (playerFled) return true;

        if (enemyParty.IsDefeated())
        {
            Log("[BATTLE] WIN");
            playerWon = true;
            return true;
        }

        if (playerParty.IsDefeated())
        {
            Log("[BATTLE] LOSE");
            playerWon = false;
            return true;
        }

        return false;
    }

    void EndBattle()
    {
        Log("[BATTLE] End");

        if (playerFled)
        {
            EventManager.Publish(GameEvent.BattleFlee);
        }
        else if (playerWon)
        {
            EventManager.Publish(GameEvent.BattleWin);
            // Quest objectives giờ được xử lý bởi BattleQuestTrigger qua EventManager
        }
        else
        {
            EventManager.Publish(GameEvent.BattleLose);
            // Quest objectives giờ được xử lý bởi BattleQuestTrigger qua EventManager
        }

        if (playerWon)
        {
            int aliveCount = 0;
            int levelSum = 0;
            foreach (var p in playerParty.Members)
            {
                var ps = p as PlayerStatus;
                if (ps != null && ps.IsAlive) { aliveCount++; levelSum += ps.level; }
            }
            int avgLevel = aliveCount > 0 ? Mathf.Max(1, levelSum / aliveCount) : 1;

            int totalExp = 0;
            foreach (var e in enemyParty.Members)
            {
                var es = e as EnemyStatus;
                if (es != null) totalExp += es.GetExpReward(avgLevel);
            }

            // Chia đều EXP cho số thành viên còn sống.
            int expPerPlayer = aliveCount > 0 ? Mathf.Max(1, totalExp / aliveCount) : 0;
            Log($"[EXP] Total: {totalExp} | Alive: {aliveCount} | Each: {expPerPlayer} (AvgLv {avgLevel})");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"EXP nhan duoc: {expPerPlayer} / nguoi");
            foreach (var p in playerParty.Members)
            {
                var ps = p as PlayerStatus;
                if (ps != null && ps.IsAlive)
                {
                    ps.GainExp(expPerPlayer);
                    Log($"[EXP] {ps.entityName}: +{expPerPlayer} EXP → Lv{ps.level} ({ps.currentExp}/{ps.expToNextLevel})");
                    sb.AppendLine($"{ps.entityName} → Lv{ps.level} ({ps.currentExp}/{ps.expToNextLevel})");
                }
            }

            if (BattleUI.Instance != null)
                BattleUI.Instance.SetExpResult(sb.ToString());
        }

        // Chỉ save khi thắng hoặc bỏ chạy — khi thua, RespawnAtSavePoint() sẽ hồi máu trước rồi save sau
        if (playerWon || playerFled)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SavePlayerParty();
            QuestManager.Instance?.SaveProgress();
        }

        if (InputController.Instance != null)
            InputController.Instance.UnbindBattleManager();

        if (MapManager.Instance != null)
        {
            MapManager.Instance.EndBattle(playerWon);
        }
        else
        {
            // Không có MapManager (ví dụ khi test BattleScene độc lập) → load thẳng MapScene.
            Debug.LogWarning("[BATTLE] EndBattle: MapManager.Instance == null, fallback load MapScene.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
        }
    }
}