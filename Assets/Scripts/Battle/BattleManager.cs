using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    // STATE 
    public BattleState State { get; private set; }

    // CORE DATA 
    private Party playerParty;
    private Party enemyParty;

    private TurnManager turnManager = new TurnManager();
    private List<Character> turnOrder = new List<Character>();
    private Character currentCharacter;

    // INIT BATTLE
    public void InitBattle(Party player, Party enemy)
    {
        playerParty = player;
        enemyParty = enemy;

        turnOrder = turnManager.BuildTurnOrder(playerParty, enemyParty);
        currentCharacter = turnOrder.Count > 0 ? turnOrder[0] : null;

        State = BattleState.Start;
        AdvanceState();
    }

    // TURN FLOW
    private void AdvanceState()
    {
        if (CheckEndBattle())
            return;

        if (currentCharacter == null)
            return;

        if (playerParty.Members.Contains(currentCharacter))
            State = BattleState.PlayerTurn;
        else
            State = BattleState.EnemyTurn;
    }

    public Character GetCurrentCharacter()
    {
        return currentCharacter;
    }

    // EXECUTE ACTION (HỆ MỚI)
    public bool ExecuteAttack(AttackBase attack, Character target)
    {
        if (State != BattleState.PlayerTurn && State != BattleState.EnemyTurn)
            return false;

        if (currentCharacter == null || !currentCharacter.IsAlive)
            return false;

        if (attack == null || target == null)
            return false;

        attack.Use(currentCharacter, target);

        EndTurn();
        return true;
    }

    // END TURN
    private void EndTurn()
    {
        turnOrder = turnManager.BuildTurnOrder(playerParty, enemyParty);
        currentCharacter = turnManager.GetNext(currentCharacter, turnOrder);

        AdvanceState();
    }

    // WIN / LOSE
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
