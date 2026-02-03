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
    // SỬA HÀM INIT: Nhận thêm mapLevel (Dummy Level)
    public void InitBattle(Party player, Party enemy, int mapDifficultyLevel)
    {
        playerParty = player;
        enemyParty = enemy;

        // === LOGIC MỚI: QUÁI SCALE THEO LEVEL CỦA M AP===
        foreach (var member in enemyParty.Members)
        {
            if (member is Enemy enemyUnit)
            {
                // Thay vì lấy player.level, ta dùng mapDifficultyLevel
                enemyUnit.SyncToLevel(mapDifficultyLevel);
            }
        }
        // =================================================

        turnOrder = turnManager.BuildTurnOrder(playerParty, enemyParty);
        currentCharacter = turnOrder.Count > 0 ? turnOrder[0] : null;

        State = BattleState.Start;
        AdvanceState();
    }
    
    // ... (Phần CheckEndBattle và ProcessWinRewards giữ nguyên, 
    // vì EXP nhận được đã được tính dựa trên Level của quái trong hàm SyncToLevel rồi)

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
