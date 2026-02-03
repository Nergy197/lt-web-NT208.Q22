using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    // ===== STATE =====
    public BattleState State { get; private set; }

    // ===== CORE DATA =====
    private Party playerParty;
    private Party enemyParty;

    private TurnManager turnManager = new TurnManager();
    private List<Status> turnOrder = new List<Status>();
    private Status currentUnit;

    // ===== INIT BATTLE =====
    // mapDifficultyLevel = level của map / dungeon
    public void InitBattle(Party player, Party enemy, int mapDifficultyLevel)
    {
        playerParty = player;
        enemyParty = enemy;

        // ===== SCALE ENEMY THEO MAP LEVEL =====
        foreach (var member in enemyParty.Members)
        {
            if (member is EnemyStatus enemyStatus)
            {
                enemyStatus.SetLevel(mapDifficultyLevel);
            }
        }

        // ===== BUILD TURN ORDER =====
        turnOrder = turnManager.BuildTurnOrder(playerParty, enemyParty);
        currentUnit = turnOrder.Count > 0 ? turnOrder[0] : null;

        State = BattleState.Start;
        AdvanceState();
    }

    // ===== TURN FLOW =====
    private void AdvanceState()
    {
        if (CheckEndBattle())
            return;

        if (currentUnit == null)
            return;

        if (playerParty.Members.Contains(currentUnit))
            State = BattleState.PlayerTurn;
        else
            State = BattleState.EnemyTurn;
    }

    public Status GetCurrentUnit()
    {
        return currentUnit;
    }

    // ===== EXECUTE ACTION =====
    public bool ExecuteAttack(AttackBase attack, Status target)
    {
        if (State != BattleState.PlayerTurn && State != BattleState.EnemyTurn)
            return false;

        if (currentUnit == null || !currentUnit.IsAlive)
            return false;

        if (attack == null || target == null)
            return false;

        attack.Use(currentUnit, target);

        EndTurn();
        return true;
    }

    // ===== END TURN =====
    private void EndTurn()
    {
        turnOrder = turnManager.BuildTurnOrder(playerParty, enemyParty);
        currentUnit = turnManager.GetNext(currentUnit, turnOrder);

        AdvanceState();
    }

    // ===== WIN / LOSE =====
    private bool CheckEndBattle()
    {
        if (enemyParty.IsDefeated())
        {
            State = BattleState.Win;
            return true;
        }

        if (playerParty.IsDefeated())
        {
            State = BattleState.Lose;
            return true;
        }

        return false;
    }
}
