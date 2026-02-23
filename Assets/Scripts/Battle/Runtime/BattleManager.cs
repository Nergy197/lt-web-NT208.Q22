using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public BattleState State { get; private set; }

    private Party playerParty;
    private Party enemyParty;

    private readonly List<Status> timeline = new();
    private const float BASE_DELAY = 100f;

    private Status currentUnit;
    private bool waitingForPlayerAction;
    private bool isRunning;

    public bool CanAcceptInput =>
        State == BattleState.PlayerTurn && waitingForPlayerAction;

    // ================= INIT =================
    public void InitBattle(Party player, Party enemy, int mapLevel)
    {
        Debug.Log("====================================");
        Debug.Log("[BATTLE] InitBattle()");
        Debug.Log($"Players: {player.Members.Count}, Enemies: {enemy.Members.Count}");

        playerParty = player;
        enemyParty = enemy;

        foreach (var e in enemyParty.Members.OfType<EnemyStatus>())
        {
            e.SetLevel(mapLevel);
            Debug.Log($"[BATTLE] Enemy {e.entityName} set to level {mapLevel}");
        }

        timeline.Clear();
        timeline.AddRange(playerParty.Members);
        timeline.AddRange(enemyParty.Members);

        foreach (var u in timeline)
        {
            u.ResetForBattle(BASE_DELAY);
            u.LastTargetSlot = 0;
            Debug.Log($"[BATTLE] Add to timeline: {u.entityName} (SPD={u.Spd})");
        }

        State = BattleState.Start;

        if (!isRunning)
            StartCoroutine(BattleLoop());
    }

    // ================= MAIN LOOP =================
    private IEnumerator BattleLoop()
    {
        isRunning = true;
        Debug.Log("[BATTLE] BattleLoop START");

        while (!CheckEndBattle())
        {
            currentUnit = GetNextUnit();
            if (currentUnit == null) break;

            State = currentUnit.PartyType == PartyType.Player
                ? BattleState.PlayerTurn
                : BattleState.EnemyTurn;

            Debug.Log("------------------------------------");
            Debug.Log($"[TURN] {currentUnit.entityName} | State = {State}");

            if (State == BattleState.PlayerTurn)
                yield return PlayerTurn();
            else
                yield return EnemyTurn();

            if (currentUnit.IsAlive)
            {
                currentUnit.UpdateEffectDurations();
                Debug.Log($"[TURN END] {currentUnit.entityName} effects updated");
            }

            yield return null;
        }

        isRunning = false;
        Debug.Log("=== BATTLE END ===");
    }

    private Status GetNextUnit()
    {
        timeline.RemoveAll(u => !u.IsAlive);

        if (timeline.Count == 0)
            return null;

        timeline.Sort((a, b) => a.NextTurnTime.CompareTo(b.NextTurnTime));

        var unit = timeline[0];
        unit.NextTurnTime += BASE_DELAY / Mathf.Max(1, unit.Spd);

        Debug.Log($"[TIMELINE] Next unit: {unit.entityName}");
        return unit;
    }

    // ================= PLAYER TURN =================
    private IEnumerator PlayerTurn()
    {
        RetargetIfDead_Player();

        Debug.Log($"[PLAYER TURN] {currentUnit.entityName} waiting for input");
        waitingForPlayerAction = true;

        yield return new WaitUntil(() => !waitingForPlayerAction);

        Debug.Log($"[PLAYER TURN END] {currentUnit.entityName}");
    }

    // ================= BASIC ATTACK =================
    public void SelectBasicAttack()
    {
        Debug.Log("[INPUT] SelectBasicAttack()");

        if (!CanAcceptInput)
        {
            Debug.LogWarning("[INPUT] Cannot accept input now");
            return;
        }

        var player = currentUnit as PlayerStatus;
        var target = GetEnemyBySlot(currentUnit.LastTargetSlot);

        if (player == null || target == null || player.BasicAttack == null)
        {
            Debug.LogWarning("[BASIC ATTACK] Invalid player / target / basic attack");
            EndPlayerAction();
            return;
        }

        var attack = player.BasicAttack.CreateInstance();

        Debug.Log($"[BASIC ATTACK] {player.entityName} -> {target.entityName}");
        Debug.Log($"[ATTACK DATA] {player.BasicAttack.attackName}");

        attack.Use(player, target);

        EndPlayerAction();
    }

    // ================= SKILL =================
    public void UseSkill(int skillIndex)
    {
        Debug.Log($"[INPUT] UseSkill({skillIndex})");

        if (!CanAcceptInput)
        {
            Debug.LogWarning("[INPUT] Cannot accept input now");
            return;
        }

        var player = currentUnit as PlayerStatus;
        var target = GetEnemyBySlot(currentUnit.LastTargetSlot);

        if (player == null || target == null)
        {
            Debug.LogWarning("[SKILL] Invalid player or target");
            EndPlayerAction();
            return;
        }

        var skillData = player.GetSkillByIndex(skillIndex);
        if (skillData == null)
        {
            Debug.LogWarning($"[SKILL] No skill at index {skillIndex}");
            EndPlayerAction();
            return;
        }

        var attack = skillData.CreateInstance();

        Debug.Log($"[SKILL] {player.entityName} uses {skillData.attackName}");
        Debug.Log($"[TARGET] -> {target.entityName}");

        attack.Use(player, target);

        EndPlayerAction();
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
        var target = GetEnemyBySlot(currentUnit.LastTargetSlot);
        if (target != null) return;

        int newSlot = StepToNextAliveEnemy(currentUnit.LastTargetSlot, +1);
        currentUnit.LastTargetSlot = newSlot;

        var t = enemyParty.GetMemberBySlot(newSlot);
        if (t != null)
            Debug.Log($"[RETARGET] New target: {t.entityName}");
    }

    // ================= ENEMY =================
    private IEnumerator EnemyTurn()
    {
        var target = playerParty.Members
            .OfType<PlayerStatus>()
            .FirstOrDefault(p => p.IsAlive);

        if (target != null)
        {
            Debug.Log($"[ENEMY ATTACK] {currentUnit.entityName} -> {target.entityName}");
            target.TakeDamage(currentUnit, currentUnit.Atk);
        }

        yield return null;
    }

    // ================= TARGET =================
    private EnemyStatus GetEnemyBySlot(int slot)
    {
        var e = enemyParty.GetMemberBySlot(slot) as EnemyStatus;
        return (e != null && e.IsAlive) ? e : null;
    }

    // ================= END =================
    private bool CheckEndBattle()
    {
        if (enemyParty.IsDefeated())
        {
            State = BattleState.Win;
            Debug.Log("[BATTLE RESULT] PLAYER WIN");
            return true;
        }

        if (playerParty.IsDefeated())
        {
            State = BattleState.Lose;
            Debug.Log("[BATTLE RESULT] PLAYER LOSE");
            return true;
        }

        return false;
    }

    private void EndPlayerAction()
    {
        Debug.Log($"[END ACTION] {currentUnit.entityName}");
        RetargetIfDead_Player();
        waitingForPlayerAction = false;
    }
}
