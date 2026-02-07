using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    private int turnCount = 0;
    public BattleState State { get; private set; }

    private Party playerParty;
    private Party enemyParty;

    // ===== SPEED BASED TURN =====
    private List<Status> turnVector = new List<Status>();
    private const float BASE_DELAY = 100f;

    // ===== CURRENT TURN =====
    private Status currentUnit;

    // ===== FLOW CONTROL =====
    private bool isRunning = false;
    private bool waitingForPlayerAction = false;

    // ===== INPUT GATE =====
    public bool CanAcceptInput =>
        State == BattleState.PlayerTurn && waitingForPlayerAction;

    // ================= INIT =================
    public void InitBattle(Party player, Party enemy, int mapLevel)
    {
        playerParty = player;
        enemyParty = enemy;

        foreach (var e in enemyParty.Members.OfType<EnemyStatus>())
            e.SetLevel(mapLevel);

        BuildTurnVector();

        foreach (var u in playerParty.Members.Concat(enemyParty.Members))
        {
            u.ResetForBattle(BASE_DELAY);
            u.LastTargetSlot = 0;
        }

        State = BattleState.Start;
        turnCount = 0;
        currentUnit = null;

        Debug.Log("=== BATTLE START ===");

        if (!isRunning)
            StartCoroutine(BattleLoop());
    }

    private void BuildTurnVector()
    {
        turnVector.Clear();
        turnVector.AddRange(playerParty.Members);
        turnVector.AddRange(enemyParty.Members);
    }

    // ================= SPEED CORE =================
    private Status GetNextUnit()
    {
        turnVector.Sort((a, b) => a.NextTurnTime.CompareTo(b.NextTurnTime));

        foreach (var u in turnVector)
        {
            if (!u.IsAlive)
            {
                u.NextTurnTime += BASE_DELAY * 1000;
                continue;
            }

            u.NextTurnTime += BASE_DELAY / Mathf.Max(1, u.Spd);
            return u;
        }
        return null;
    }

    // ================= MAIN LOOP =================
    private IEnumerator BattleLoop()
    {
        isRunning = true;

        while (!CheckEndBattle())
        {
            currentUnit = GetNextUnit();
            if (currentUnit == null) break;

            turnCount++;

            State = currentUnit.PartyType == PartyType.Player
                ? BattleState.PlayerTurn
                : BattleState.EnemyTurn;

            Debug.Log($"================ TURN {turnCount} ================");
            Debug.Log($"TURN UNIT: {currentUnit.entityName} (SPD={currentUnit.Spd})");

            LogBattleState();

            if (State == BattleState.PlayerTurn)
            {
                // 🔥 PLAYER RETARGET TRƯỚC KHI INPUT
                RetargetIfDead_Player();
                DebugLogCurrentTarget();

                waitingForPlayerAction = true;
                yield return new WaitUntil(() => !waitingForPlayerAction || CheckEndBattle());
            }
            else
            {
                EnemyAutoAttack();
                yield return null;
            }

            yield return null;
        }

        isRunning = false;
        Debug.Log("=== BATTLE END ===");
    }

    // ================= PLAYER =================
    public void SelectBasicAttack()
    {
        if (!CanAcceptInput) return;

        var target = GetEnemyBySlot(currentUnit.LastTargetSlot);
        if (target == null)
        {
            Debug.Log("[ATTACK] No valid target");
            waitingForPlayerAction = false;
            return;
        }

        Debug.Log($"[PLAYER ATTACK] {currentUnit.entityName} -> {target.entityName} (slot {currentUnit.LastTargetSlot})");
        target.TakeDamage(currentUnit, currentUnit.Atk);

        // 🔥 RETARGET SAU KHI ĐÁNH
        RetargetIfDead_Player();

        waitingForPlayerAction = false;
    }

    public void ChangeTargetInput(int dir)
    {
        if (!CanAcceptInput) return;

        int oldSlot = currentUnit.LastTargetSlot;
        int newSlot = StepToNextAliveEnemy(oldSlot, dir);
        currentUnit.LastTargetSlot = newSlot;

        var t = enemyParty.GetMemberBySlot(newSlot);
        if (t != null)
            Debug.Log($"[TARGET CHANGE] {currentUnit.entityName}: {oldSlot} -> {newSlot} ({t.entityName})");
    }

    // ================= TARGET CORE =================
    private int StepToNextAliveEnemy(int start, int dir)
    {
        int count = enemyParty.Members.Count;
        int index = start;

        for (int i = 0; i < count; i++)
        {
            index = (index + dir + count) % count;
            var e = enemyParty.GetMemberBySlot(index) as EnemyStatus;
            if (e != null && e.IsAlive)
                return index;
        }
        return start;
    }

    private EnemyStatus GetEnemyBySlot(int slot)
    {
        var e = enemyParty.GetMemberBySlot(slot) as EnemyStatus;
        if (e != null && e.IsAlive)
            return e;

        return null; // ❗ KHÔNG fallback
    }

    // ================= PLAYER RETARGET =================
    private void RetargetIfDead_Player()
    {
        int slot = currentUnit.LastTargetSlot;
        var target = enemyParty.GetMemberBySlot(slot) as EnemyStatus;

        if (target != null && target.IsAlive)
            return;

        int newSlot = StepToNextAliveEnemy(slot, +1);
        currentUnit.LastTargetSlot = newSlot;

        var newTarget = enemyParty.GetMemberBySlot(newSlot);
        if (newTarget != null)
            Debug.Log($"[RETARGET PLAYER] {currentUnit.entityName} -> {newTarget.entityName} (slot {newSlot})");
    }

    // ================= ENEMY =================
    private void EnemyAutoAttack()
    {
        if (!currentUnit.IsAlive) return;

        var target = RetargetEnemyTarget();
        if (target == null) return;

        Debug.Log($"[ENEMY ATTACK] {currentUnit.entityName} -> {target.entityName}");
        target.TakeDamage(currentUnit, currentUnit.Atk);
    }

    // 🔥 ENEMY RETARGET MỖI LƯỢT
    private PlayerStatus RetargetEnemyTarget()
    {
        for (int i = 0; i < playerParty.Members.Count; i++)
        {
            var p = playerParty.GetMemberBySlot(i) as PlayerStatus;
            if (p != null && p.IsAlive)
                return p;
        }
        return null;
    }

    // ================= DEBUG =================
    private void DebugLogCurrentTarget()
    {
        int slot = currentUnit.LastTargetSlot;
        var t = enemyParty.GetMemberBySlot(slot);

        if (t != null)
            Debug.Log($"[CURRENT TARGET] {currentUnit.entityName} -> slot {slot}: {t.entityName}");
        else
            Debug.Log($"[CURRENT TARGET] {currentUnit.entityName} has no valid target");
    }

    private void LogBattleState()
    {
        foreach (var p in playerParty.Members.OfType<PlayerStatus>())
            Debug.Log($"[PLAYER] {p.entityName} HP {p.currentHP}/{p.MaxHP}");

        foreach (var e in enemyParty.Members.OfType<EnemyStatus>())
            Debug.Log($"[ENEMY] {e.entityName} HP {e.currentHP}/{e.MaxHP}");
    }

    // ================= END =================
    private bool CheckEndBattle()
    {
        if (enemyParty.IsDefeated())
        {
            State = BattleState.Win;
            Debug.Log("PLAYER WIN");
            return true;
        }

        if (playerParty.IsDefeated())
        {
            State = BattleState.Lose;
            Debug.Log("PLAYER LOSE");
            return true;
        }
        return false;
    }
}
