using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    private int turnCount = 0;

    public BattleState State { get; private set; }

    private Party playerParty;
    private Party enemyParty;

    private Queue<Status> turnOrder = new Queue<Status>();
    private Status currentUnit;

    private int targetIndex = 0;

    // ================= INIT =================
    public void InitBattle(Party player, Party enemy, int mapLevel)
    {
        playerParty = player;
        enemyParty = enemy;

        foreach (var e in enemyParty.Members.OfType<EnemyStatus>())
            e.SetLevel(mapLevel);

        turnOrder = new Queue<Status>(
            playerParty.Members.Concat(enemyParty.Members)
        );

        currentUnit = turnOrder.Peek();
        State = BattleState.Start;

        Debug.Log("=== BATTLE START ===");
        NextTurn();
    }

    // ================= TURN =================
    private void NextTurn()
    {
        if (CheckEndBattle()) return;

        turnCount++;

        currentUnit = turnOrder.Dequeue();
        turnOrder.Enqueue(currentUnit);

        State = currentUnit is PlayerStatus
            ? BattleState.PlayerTurn
            : BattleState.EnemyTurn;

        Debug.Log($"================ TURN {turnCount} ================");
        Debug.Log($"▶ {currentUnit.entityName} TURN ({State})");

        LogBattleState();

        if (State == BattleState.EnemyTurn)
            EnemyAutoAttack();
    }

    // ================= PLAYER =================
    public void SelectBasicAttack()
    {
        if (State != BattleState.PlayerTurn) return;

        var target = GetCurrentTargetEnemy();
        if (target == null) return;

        Debug.Log($"🗡 PLAYER attacks {target.entityName}");
        target.TakeDamage(currentUnit, currentUnit.Atk);

        NextTurn();
    }

    public void ChangeTargetInput(int dir)
    {
        if (State != BattleState.PlayerTurn) return;

        var enemies = enemyParty.Members.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;

        targetIndex = (targetIndex + dir + enemies.Count) % enemies.Count;
        Debug.Log($"🎯 Target → {enemies[targetIndex].entityName}");
    }

    private EnemyStatus GetCurrentTargetEnemy()
    {
        var enemies = enemyParty.Members.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return null;

        targetIndex = Mathf.Clamp(targetIndex, 0, enemies.Count - 1);
        return enemies[targetIndex] as EnemyStatus;
    }

    // ================= ENEMY =================
    private void EnemyAutoAttack()
    {
        var target = playerParty.Members
            .OfType<PlayerStatus>()
            .FirstOrDefault(p => p.IsAlive);

        if (target == null) return;

        Debug.Log($"👹 ENEMY attacks {target.entityName}");
        target.TakeDamage(currentUnit, currentUnit.Atk);

        NextTurn();
    }

    // ================= LOG =================
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
            Debug.Log("🏆 PLAYER WIN");
            return true;
        }

        if (playerParty.IsDefeated())
        {
            State = BattleState.Lose;
            Debug.Log("💀 PLAYER LOSE");
            return true;
        }
        return false;
    }
}
