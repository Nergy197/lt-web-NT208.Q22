using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    // Expose cho PlayerAttack.FindLowestHPAlly()
    public static BattleManager Instance { get; private set; }
    public Party PlayerParty => playerParty;

    private Party playerParty;
    private Party enemyParty;

    private List<Status> timeline = new();

    private Status currentUnit;

    private bool waitingForPlayerAction = false;

    // FIX: Cở cờ xử lý attack coroutine chưa kết thúc
    private bool waitingForAttackFinish = false;

    // BUG FIX: Track win/lose để trao EXP đúng lúc
    private bool playerWon = false;
    private bool playerFled = false;

    private const float BASE_DELAY = 100f;

    // FIX 3: Target selection
    private int currentTargetIndex = 0;
    private int currentAllyTargetIndex = 0;

    // ================= DEBUG HELPER =================

    void Log(string msg)
    {
        Debug.Log(msg);

        if (BattleDebugUI.Instance != null)
            BattleDebugUI.Instance.Log(msg);
    }

    // ================= START =================

    IEnumerator Start()
    {
        Instance = this;

        Log("[BATTLE] Start");

        // Đăng ký lắng nghe event attack kết thúc
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
        // Hủy đăng ký khi bị destroy (chuyển scene)
        BattleEvents.OnAttackFinished -= OnAttackFinished;

        if (Instance == this) Instance = null;
    }

    void OnAttackFinished()
    {
        waitingForAttackFinish = false;
        Log("[ATTACK] Finished");
    }

    // ================= LOAD ENEMY =================

    void LoadEnemy()
    {
        enemyParty = new Party(PartyType.Enemy);

        foreach (var data in MapManager.Instance.currentEnemies)
        {
            var e = data.CreateStatus();

            enemyParty.AddMember(e);

            Log("[BATTLE] Spawn enemy: " + e.entityName);
        }
    }

    // ================= INIT =================

    void InitBattle()
    {
        timeline.Clear();

        AddParty(playerParty);

        AddParty(enemyParty);

        Log("[BATTLE] Init Complete");

        StartCoroutine(BattleLoop());
    }

    void AddParty(Party party)
    {
        foreach (var s in party.Members)
        {
            s.ResetForBattle(BASE_DELAY);

            timeline.Add(s);

            Log("[BATTLE] Added: " + s.entityName);
        }
    }

    // ================= LOOP =================

    IEnumerator BattleLoop()
    {
        while (true)
        {
            if (CheckEndBattle())
                break;

            currentUnit = GetNextUnit();

            if (!currentUnit.IsAlive)
                continue;

            Log("[TURN] " + currentUnit.entityName);

            // BUG FIX: Xử lý Poison damage và Stun trước khi unit hành động
            bool isStunned = ProcessTurnEffects(currentUnit);

            if (isStunned)
            {
                Log($"[STUN] {currentUnit.entityName} bị Stun, mất lượt!");
                currentUnit.UpdateEffectDurations();
                continue;
            }

            if (currentUnit is PlayerStatus)
                yield return PlayerTurn();
            else
                yield return EnemyTurn();

            // BUG FIX: Cập nhật thời gian còn lại của status effects sau mỗi lượt
            currentUnit.UpdateEffectDurations();
        }

        EndBattle();
    }

    // BUG FIX: Xử lý Poison và Stun mỗi đầu lượt
    // Trả về true nếu unit bị Stun (mất lượt)
    bool ProcessTurnEffects(Status unit)
    {
        bool stunned = false;

        // Đọc danh sách effects qua reflection-free cách: dùng interface
        // Vì activeEffects là private, ta xử lý thông qua method mới trong Status
        var effects = unit.GetActiveEffects();

        foreach (var effect in effects)
        {
            switch (effect.effectType)
            {
                case StatusEffectType.Poison:
                    if (unit.IsAlive && effect.value > 0)
                    {
                        int poisonDmg = effect.value;
                        unit.TakePoisonDamage(poisonDmg);
                        Log($"[POISON] {unit.entityName} mất {poisonDmg} HP (còn {unit.currentHP})");
                    }
                    break;

                case StatusEffectType.Stun:
                    stunned = true;
                    break;
            }
        }

        return stunned;
    }

    // ================= PLAYER =================

    IEnumerator PlayerTurn()
    {
        waitingForPlayerAction = true;
        waitingForAttackFinish = false;

        Log("[TURN] Waiting Player Action");

        // Bước 1: Đợi player chọn hành động
        yield return new WaitUntil(() => !waitingForPlayerAction);

        // Bước 2: Đợi attack coroutine chạy xong (nếu có)
        if (waitingForAttackFinish)
        {
            Log("[TURN] Waiting for attack to finish...");
            yield return new WaitUntil(() => !waitingForAttackFinish);
        }
    }

    public void SelectBasicAttack()
    {
        if (!waitingForPlayerAction) return; // chặn input khi không phải lượt player

        var player = currentUnit as PlayerStatus;

        var enemy = GetEnemyTarget();

        if (enemy == null)
        {
            Log("[ERROR] No enemy target");
            waitingForPlayerAction = false;
            return;
        }

        Log("[ACTION] Attack → " + enemy.entityName);

        // Đánh dấu đang chờ attack coroutine TRƯỚC khi gọi .Use()
        waitingForAttackFinish = true;
        player.BasicAttack
            .CreateInstance()
            .Use(player, enemy);

        // Cho phép BattleLoop đi tiếp — PlayerTurn sẽ chờ nốt waitingForAttackFinish
        waitingForPlayerAction = false;
    }

    public void UseSkill(int index)
    {
        if (!waitingForPlayerAction) return; // chặn input khi không phải lượt player

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
            waitingForPlayerAction = false;
            return;
        }

        var skill = player.GetSkillByIndex(index);

        if (skill == null)
        {
            Log("[ERROR] Skill[" + index + "] not found (player has " + player.SkillCount + " skills)");
            waitingForPlayerAction = false;
            return;
        }

        if (!player.CanUseAP(skill.apCost))
        {
            Log("[ERROR] Not enough AP: need " + skill.apCost + ", have " + player.currentAP);
            waitingForPlayerAction = false;
            return;
        }

        Log("[ACTION] Skill[" + index + "] → " + skill.attackName + " (AP: " + skill.apCost + ")");

        // Đánh dấu đang chờ attack coroutine TRƯỚC khi gọi .Use()
        waitingForAttackFinish = true;
        skill.CreateInstance()
            .Use(player, enemy);

        waitingForPlayerAction = false;
    }

    public void RequestParry()
    {
        foreach (var p in playerParty.Members)
        {
            var ps = p as PlayerStatus;
            if (ps != null && ps.IsAlive)
            {
                ps.RequestParry();
            }
        }
        Log("[ACTION] Parry Requested");
    }

    // ================= FLEE =================

    public void TryFlee()
    {
        if (!waitingForPlayerAction) return;

        var player = currentUnit as PlayerStatus;
        if (player == null) return;

        Log("[ACTION] Try Flee");

        // Tìm quái vật nhanh nhất
        int maxEnemySpd = 1;
        foreach (var e in enemyParty.Members)
        {
            if (e.IsAlive && e.Spd > maxEnemySpd)
                maxEnemySpd = e.Spd;
        }

        // Tỉ lệ cơ bản 50%, cộng trừ 5% cho mỗi điểm chênh lệch Speed
        float chance = 0.5f + (player.Spd - maxEnemySpd) * 0.05f;
        chance = Mathf.Clamp(chance, 0.1f, 0.95f); // Tối thiểu 10%, tối đa 95%

        if (Random.value <= chance)
        {
            Log("[FLEE] Success! Escaping battle...");
            playerFled = true;
            // Không đợi attack finish vì flee kết thúc battle ngay
        }
        else
        {
            Log("[FLEE] Failed! Turn wasted.");
        }

        waitingForPlayerAction = false;
    }

    // ================= ENEMY =================

    IEnumerator EnemyTurn()
    {
        var enemy = currentUnit as EnemyStatus;

        var player = GetAlivePlayer();

        if (player == null)
        {
            Log("[ENEMY] No alive player target");
            yield break;
        }

        Log("[ENEMY] Attack → " + player.entityName);

        // Đánh dấu đợi attack coroutine (giống PlayerTurn)
        // Parry window nằm BÊN TRONG EnemyAttack.Execute(), nên phải đợi nó xong
        waitingForAttackFinish = true;

        enemy.GetRandomAttack()
            .CreateInstance()
            .Use(enemy, player);

        // Đợi parry window + toàn bộ attack hoàn thành
        // (OnAttackFinished sẽ set waitingForAttackFinish = false)
        yield return new WaitUntil(() => !waitingForAttackFinish);
    }

    // ================= TARGET =================

    EnemyStatus GetEnemyTarget()
    {
        // FIX 3: Trả về enemy theo currentTargetIndex, auto-clamp khi target chết
        var alive = new System.Collections.Generic.List<EnemyStatus>();
        foreach (var e in enemyParty.Members)
            if (e.IsAlive) alive.Add(e as EnemyStatus);

        if (alive.Count == 0) return null;

        currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, alive.Count - 1);
        return alive[currentTargetIndex];
    }

    PlayerStatus GetAlivePlayer()
    {
        foreach (var p in playerParty.Members)
            if (p.IsAlive)
                return p as PlayerStatus;

        return null;
    }

    public PlayerStatus GetAllyTarget()
    {
        var alive = new System.Collections.Generic.List<PlayerStatus>();
        foreach (var p in playerParty.Members)
            if (p.IsAlive) alive.Add(p as PlayerStatus);

        if (alive.Count == 0) return null;

        currentAllyTargetIndex = Mathf.Clamp(currentAllyTargetIndex, 0, alive.Count - 1);
        return alive[currentAllyTargetIndex];
    }

    public void ChangeTargetInput(int dir)
    {
        // 1. Cycle qua danh sách enemy còn sống
        var aliveEnemies = new System.Collections.Generic.List<EnemyStatus>();
        foreach (var e in enemyParty.Members)
            if (e.IsAlive) aliveEnemies.Add(e as EnemyStatus);

        if (aliveEnemies.Count > 0)
        {
            currentTargetIndex = (currentTargetIndex + dir + aliveEnemies.Count) % aliveEnemies.Count;
            Log("[TARGET ENEMY] → " + aliveEnemies[currentTargetIndex].entityName);
        }

        // 2. Cycle qua danh sách ally còn sống (dùng chung 1 input)
        var aliveAllies = new System.Collections.Generic.List<PlayerStatus>();
        foreach (var p in playerParty.Members)
            if (p.IsAlive) aliveAllies.Add(p as PlayerStatus);

        if (aliveAllies.Count > 0)
        {
            currentAllyTargetIndex = (currentAllyTargetIndex + dir + aliveAllies.Count) % aliveAllies.Count;
            Log("[TARGET ALLY] → " + aliveAllies[currentAllyTargetIndex].entityName);
        }
    }

    // ================= TIMELINE =================

    Status GetNextUnit()
    {
        timeline.Sort((a, b) =>
            a.NextTurnTime.CompareTo(b.NextTurnTime));

        var unit = timeline[0];

        unit.NextTurnTime += BASE_DELAY / unit.Spd;

        return unit;
    }

    // ================= END =================

    bool CheckEndBattle()
    {
        if (playerFled)
        {
            // playerWon vẫn false -> không nhận EXP
            return true;
        }

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

        // BUG FIX: Trao EXP cho toàn bộ player còn sống nếu thắng
        if (playerWon)
        {
            int totalExp = 0;

            foreach (var e in enemyParty.Members)
            {
                var es = e as EnemyStatus;
                if (es != null)
                    totalExp += es.GetExpReward();
            }

            Log($"[EXP] Earned {totalExp} EXP total");

            foreach (var p in playerParty.Members)
            {
                var ps = p as PlayerStatus;
                if (ps != null && ps.IsAlive)
                {
                    ps.GainExp(totalExp);
                    Log($"[EXP] {ps.entityName}: +{totalExp} EXP (Level {ps.level})");
                }
            }
        }

        GameManager.Instance.SavePlayerParty();

        InputController.Instance.UnbindBattleManager();

        MapManager.Instance.EndBattle();
    }
}