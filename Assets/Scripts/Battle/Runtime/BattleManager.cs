using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    /// <summary>Expose để PlayerAttack.ResolveEffectTarget có thể lấy ally target.</summary>
    public Party PlayerParty => playerParty;

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

    [Header("UI System")]
    [SerializeField] private GameObject targetCursor;
    [SerializeField] private Vector3 cursorOffset = new Vector3(0, 0f, 0);

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

        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.isLoaded);

        playerParty = GameManager.Instance.GetPlayerParty();
        LoadEnemy();
        InputController.Instance.BindBattleManager(this);
        InitBattle();
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
            targetCursor.transform.position = currentSelectedTarget.SpawnedModel.transform.position + cursorOffset;
        }
        else
        {
            if (targetCursor.activeSelf) targetCursor.SetActive(false);
        }
    }

    void LoadEnemy()
    {
        enemyParty = new Party(PartyType.Enemy);
        // Dùng CreateEnemyStatuses() để enemy được SetLevel + apply map effects đúng cách
        foreach (var e in MapManager.Instance.CreateEnemyStatuses())
        {
            enemyParty.AddMember(e);
            Log($"[BATTLE] Spawn enemy: {e.entityName} Lv{e.level}");
        }
    }

    void InitBattle()
    {
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
            member.SpawnedModel = model;
            member.BattleSlotId = i;

            Log($"[SPAWN] {member.entityName} → {spawnPos}");
        }
    }

    IEnumerator BattleLoop()
    {
        while (true)
        {
            if (CheckEndBattle()) break;

            currentUnit = turnManager.GetNextUnit();
            if (currentUnit == null || !currentUnit.IsAlive) continue;

            Log("[TURN] " + currentUnit.entityName);

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

    IEnumerator PlayerTurn()
    {
        waitingForPlayerAction = true;
        waitingForAttackFinish = false;
        Log("[TURN] Waiting Player Action");

        // Bước 1: Đợi player chọn hành động (attack, skill, flee…).
        yield return new WaitUntil(() => !waitingForPlayerAction);

        // Bước 2: Đợi coroutine attack chạy đến phase Finished.
        if (waitingForAttackFinish)
        {
            Log("[TURN] Waiting for attack to finish...");
            yield return new WaitUntil(() => !waitingForAttackFinish);
        }
    }

    public void SelectBasicAttack()
    {
        if (!waitingForPlayerAction) return;

        var player = currentUnit as PlayerStatus;
        var enemy = GetEnemyTarget();

        if (enemy == null)
        {
            Log("[ERROR] No enemy target");
            return;
        }

        Log("[ACTION] Attack → " + enemy.entityName);

        // Phải set cờ trước khi gọi Use() vì coroutine attack chạy ngay trong frame đó.
        waitingForAttackFinish = true;
        player.BasicAttack.CreateInstance().Use(player, enemy);

        waitingForPlayerAction = false;
    }

    public void UseSkill(int index)
    {
        if (!waitingForPlayerAction) return;

        var player = currentUnit as PlayerStatus;
        if (player == null)
        {
            Log("[ERROR] Current unit is not a player");
            return;
        }

        var enemy = GetEnemyTarget();
        if (enemy == null)
        {
            Log("[ERROR] No enemy target");
            // Giữ waitingForPlayerAction = true → player chọn lại
            return;
        }

        var skill = player.GetSkillByIndex(index);
        if (skill == null)
        {
            Log($"[ERROR] Skill[{index}] not found (player has {player.SkillCount} skills)");
            // Giữ waitingForPlayerAction = true → player chọn lại
            return;
        }

        if (!player.CanUseAP(skill.apCost))
        {
            Log($"[ERROR] Not enough AP: need {skill.apCost}, have {player.currentAP}");
            // Giữ waitingForPlayerAction = true → player chọn lại
            return;
        }

        Log($"[ACTION] Skill[{index}] → {skill.attackName} (AP: {skill.apCost})");

        waitingForAttackFinish = true;
        skill.CreateInstance().Use(player, enemy);

        waitingForPlayerAction = false;
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

        // Tỉ lệ bỏ chạy cơ bản 50%, điều chỉnh ±5% cho mỗi điểm chênh lệch Speed.
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

    IEnumerator EnemyTurn()
    {
        var enemy = currentUnit as EnemyStatus;
        var player = GetAlivePlayer();

        if (player == null)
        {
            Log("[ENEMY] No alive player target");
            yield break;
        }

        var attackData = enemy.GetRandomAttack();
        if (attackData == null)
        {
            Log($"[ENEMY] {enemy.entityName} has no attacks configured – skipping turn.");
            yield break;
        }

        Log("[ENEMY] Attack → " + player.entityName);

        // Set cờ trước khi Use() vì parry window chạy bên trong EnemyAttack.Execute().
        waitingForAttackFinish = true;
        attackData.CreateInstance().Use(enemy, player);

        // Đợi toàn bộ attack (wind-up + parry window + impact + recovery) hoàn thành.
        yield return new WaitUntil(() => !waitingForAttackFinish);
    }

    EnemyStatus GetEnemyTarget()
    {
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
        foreach (var p in playerParty.Members)
            if (p.IsAlive) return p as PlayerStatus;
        return null;
    }

    public PlayerStatus GetAllyTarget()
    {
        var alive = new List<PlayerStatus>();
        foreach (var p in playerParty.Members)
            if (p.IsAlive) alive.Add(p as PlayerStatus);

        if (alive.Count == 0) return null;

        currentAllyTargetIndex = Mathf.Clamp(currentAllyTargetIndex, 0, alive.Count - 1);
        return alive[currentAllyTargetIndex];
    }

    public void ChangeTargetInput(int dir)
    {
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
        }
        else if (isTargetingAlly && aliveAllies.Count > 0)
        {
            currentAllyTargetIndex = (currentAllyTargetIndex + dir + aliveAllies.Count) % aliveAllies.Count;
            Log("[TARGET ALLY] → " + aliveAllies[currentAllyTargetIndex].entityName);
        }
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

            // Hoàn thành objective "Dẫn quân ra trận" và "Đánh giá thực lực địch" (Q002).
            QuestManager.Instance?.CompleteObjective("Q002", "O2");
            QuestManager.Instance?.CompleteObjective("Q002", "O3");

            // Đã đủ sức mạnh dù không thua → bỏ qua yêu cầu thua trận của Q003.O1.
            QuestManager.Instance?.CompleteObjective("Q003", "O1");
        }
        else
        {
            EventManager.Publish(GameEvent.BattleLose);

            // Thua trận → trận chiến vẫn diễn ra (O2, O3) và nếm mùi thất bại (O1).
            QuestManager.Instance?.CompleteObjective("Q002", "O2");
            QuestManager.Instance?.CompleteObjective("Q002", "O3");
            QuestManager.Instance?.CompleteObjective("Q003", "O1");
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

            foreach (var p in playerParty.Members)
            {
                var ps = p as PlayerStatus;
                if (ps != null && ps.IsAlive)
                {
                    ps.GainExp(expPerPlayer);
                    Log($"[EXP] {ps.entityName}: +{expPerPlayer} EXP → Lv{ps.level} ({ps.currentExp}/{ps.expToNextLevel})");
                }
            }
        }

        GameManager.Instance.SavePlayerParty();
        QuestManager.Instance?.SaveProgress();

        InputController.Instance.UnbindBattleManager();
        MapManager.Instance.EndBattle(playerWon);
    }
}