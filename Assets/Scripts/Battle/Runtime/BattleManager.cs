using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    // ================= STATE =================
    public BattleState State { get; private set; }
    private int turnCount = 0;

    private Party playerParty;
    private Party enemyParty;

    // ================= TIMELINE =================
    private readonly List<Status> timeline = new();
    private const float BASE_DELAY = 100f;

    // ================= TURN =================
    private Status currentUnit;
    private bool waitingForPlayerAction;
    private bool isRunning;

    public bool CanAcceptInput =>
        State == BattleState.PlayerTurn && waitingForPlayerAction;

    // ================= INIT =================
    public void InitBattle(Party player, Party enemy, int mapLevel)
    {
        playerParty = player;
        enemyParty = enemy;

        foreach (var e in enemyParty.Members.OfType<EnemyStatus>())
            e.SetLevel(mapLevel);

        BuildTimeline();

        foreach (var u in timeline)
        {
            u.ResetForBattle(BASE_DELAY);
            u.LastTargetSlot = 0;
        }

        State = BattleState.Start;
        turnCount = 0;

        Debug.Log("=== BATTLE START ===");

        if (!isRunning)
            StartCoroutine(BattleLoop());
    }

    // ================= TIMELINE =================
    private void BuildTimeline()
    {
        timeline.Clear();
        timeline.AddRange(playerParty.Members);
        timeline.AddRange(enemyParty.Members);
    }

    private void RemoveDeadFromTimeline()
    {
        timeline.RemoveAll(u => !u.IsAlive);
    }

    private Status GetNextUnit()
    {
        RemoveDeadFromTimeline();
        if (timeline.Count == 0)
            return null;

        timeline.Sort((a, b) => a.NextTurnTime.CompareTo(b.NextTurnTime));

        Status unit = timeline[0];
        unit.NextTurnTime += BASE_DELAY / Mathf.Max(1, unit.Spd);

        return unit;
    }

    // ================= MAIN LOOP =================
    private IEnumerator BattleLoop()
    {
        isRunning = true;

        while (!CheckEndBattle())
        {
            currentUnit = GetNextUnit();
            if (currentUnit == null)
                break;

            turnCount++;
            State = currentUnit.PartyType == PartyType.Player
                ? BattleState.PlayerTurn
                : BattleState.EnemyTurn;

            Debug.Log($"================ TURN {turnCount} ================");
            Debug.Log($"TURN: {currentUnit.entityName} (SPD={currentUnit.Spd})");

            LogBattleState();

            if (State == BattleState.PlayerTurn)
                yield return PlayerTurn();
            else
                yield return EnemyTurn();

            if (currentUnit != null && currentUnit.IsAlive)
                currentUnit.UpdateEffectDurations();

            yield return null;
        }

        isRunning = false;
        Debug.Log("=== BATTLE END ===");
    }

    // ================= PLAYER TURN =================
    private IEnumerator PlayerTurn()
    {
        RetargetIfDead_Player();
        DebugLogCurrentTarget();

        waitingForPlayerAction = true;
        yield return new WaitUntil(() =>
            !waitingForPlayerAction || CheckEndBattle());
    }

    // ================= BASIC ATTACK =================
    public void SelectBasicAttack()
    {
        if (!CanAcceptInput) return;

        var target = GetEnemyBySlot(currentUnit.LastTargetSlot);
        if (target == null)
        {
            waitingForPlayerAction = false;
            return;
        }

        Debug.Log($"[PLAYER ATTACK] {currentUnit.entityName} -> {target.entityName}");
        target.TakeDamage(currentUnit, currentUnit.Atk);

        RetargetIfDead_Player();
        waitingForPlayerAction = false;
    }

    // ================= SKILL =================
    public void UseSkill(int skillIndex)
    {
        if (!CanAcceptInput) return;

        var player = currentUnit as PlayerStatus;
        if (player == null)
        {
            waitingForPlayerAction = false;
            return;
        }

        // 1. lấy skill data
        var skillData = player.GetSkillByIndex(skillIndex);
        if (skillData == null)
        {
            Debug.LogWarning($"[USE SKILL] No skill at index {skillIndex}");
            waitingForPlayerAction = false;
            return;
        }

        // 2. lấy target
        var target = GetEnemyBySlot(currentUnit.LastTargetSlot);
        if (target == null)
        {
            waitingForPlayerAction = false;
            return;
        }

        // 3. tạo runtime attack
        PlayerAttack attack = skillData.CreateInstance();

        // 4. check dùng được không
        if (!player.CanUseAP(attack.apCost))
        {
            Debug.LogWarning(
                $"[USE SKILL] Not enough AP to use {attack.Name} (Cost: {attack.apCost}, Current: {player.currentAP})"
            );
            waitingForPlayerAction = false;
            return;
        }

        Debug.Log(
            $"[PLAYER SKILL] {player.entityName} uses {attack.Name} on {target.entityName}"
        );

        // 5. chạy coroutine
        StartCoroutine(RunPlayerAttack(attack, player, target));
    }

    private IEnumerator RunPlayerAttack(
        PlayerAttack attack,
        PlayerStatus player,
        EnemyStatus target
    )
    {
        attack.Use(player, target);
        yield return new WaitForSeconds(0.2f);

        RetargetIfDead_Player();
        waitingForPlayerAction = false;

    }

    // ================= TARGET INPUT =================
    public void ChangeTargetInput(int dir)
    {
        if (!CanAcceptInput) return;

        int old = currentUnit.LastTargetSlot;
        int next = StepToNextAliveEnemy(old, dir);
        currentUnit.LastTargetSlot = next;

        var t = enemyParty.GetMemberBySlot(next);
        if (t != null)
            Debug.Log($"[TARGET] {currentUnit.entityName}: {old} -> {next} ({t.entityName})");
    }

    // ================= ENEMY TURN =================
    private IEnumerator EnemyTurn()
    {
        if (!currentUnit.IsAlive)
            yield break;

        var target = RetargetEnemyTarget();
        if (target != null)
        {
            Debug.Log($"[ENEMY ATTACK] {currentUnit.entityName} -> {target.entityName}");
            target.TakeDamage(currentUnit, currentUnit.Atk);
        }

        yield return null;
    }

    private PlayerStatus RetargetEnemyTarget()
    {
        foreach (var p in playerParty.Members.OfType<PlayerStatus>())
            if (p.IsAlive)
                return p;

        return null;
    }

    // ================= TARGET HELPERS =================
    private EnemyStatus GetEnemyBySlot(int slot)
    {
        var e = enemyParty.GetMemberBySlot(slot) as EnemyStatus;
        return (e != null && e.IsAlive) ? e : null;
    }

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

    private void RetargetIfDead_Player()
    {
        int slot = currentUnit.LastTargetSlot;
        var target = enemyParty.GetMemberBySlot(slot) as EnemyStatus;

        if (target != null && target.IsAlive)
            return;

        int newSlot = StepToNextAliveEnemy(slot, +1);
        currentUnit.LastTargetSlot = newSlot;

        var t = enemyParty.GetMemberBySlot(newSlot);
        if (t != null)
            Debug.Log($"[RETARGET PLAYER] -> {t.entityName}");
    }

    // ================= DEBUG =================
    private void DebugLogCurrentTarget()
    {
        var t = enemyParty.GetMemberBySlot(currentUnit.LastTargetSlot);
        if (t != null)
            Debug.Log($"[CURRENT TARGET] {currentUnit.entityName} -> {t.entityName}");
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
