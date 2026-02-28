using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public BattleState State { get; private set; }

    private Party playerParty;
    private Party enemyParty;

    private List<Status> timeline = new List<Status>();

    private Status currentUnit;

    private bool waitingForPlayerAction;

    private bool isRunning;

    private const float BASE_DELAY = 100f;



    // ================= INIT =================

    public void InitBattle(Party player, Party enemy, int mapLevel)
    {
        Debug.Log("=== INIT BATTLE ===");

        StopAllCoroutines();

        playerParty = player;
        enemyParty = enemy;

        waitingForPlayerAction = false;

        timeline.Clear();

        AddPartyToTimeline(playerParty);
        AddPartyToTimeline(enemyParty);

        State = BattleState.Start;

        StartCoroutine(BattleLoop());
    }



    private void AddPartyToTimeline(Party party)
    {
        foreach (Status s in party.Members)
        {
            s.ResetForBattle(BASE_DELAY);

            s.LastTargetSlot = 0;

            timeline.Add(s);

            Debug.Log("Add timeline: " + s.entityName);
        }
    }



    // ================= MAIN LOOP =================

    private IEnumerator BattleLoop()
    {
        isRunning = true;

        while (true)
        {

            if (CheckEndBattle())
                break;


            currentUnit = GetNextUnit();


            if (currentUnit == null)
                break;



            if (currentUnit.PartyType == PartyType.Player)
            {
                State = BattleState.PlayerTurn;

                yield return PlayerTurn();
            }
            else
            {
                State = BattleState.EnemyTurn;

                yield return EnemyTurn();
            }



            if (currentUnit.IsAlive)
                currentUnit.UpdateEffectDurations();


            yield return null;
        }


        isRunning = false;

        Debug.Log("=== BATTLE END ===");
    }



    // ================= TIMELINE =================

    private Status GetNextUnit()
    {
        RemoveDeadFromTimeline();


        if (timeline.Count == 0)
            return null;


        timeline.Sort((a, b) =>
            a.NextTurnTime.CompareTo(b.NextTurnTime));


        Status unit = timeline[0];


        float spd = Mathf.Max(0.1f, unit.Spd);


        unit.NextTurnTime += BASE_DELAY / spd;


        Debug.Log("Next turn: " + unit.entityName);


        return unit;
    }



    private void RemoveDeadFromTimeline()
    {
        timeline.RemoveAll(unit => !unit.IsAlive);
    }



    // ================= PLAYER TURN =================

    private IEnumerator PlayerTurn()
    {
        RetargetIfDead();

        waitingForPlayerAction = true;

        Debug.Log("Player turn: waiting input");

        yield return new WaitUntil(() => waitingForPlayerAction == false);

        Debug.Log("Player turn end");
    }



    private void EndPlayerAction()
    {
        waitingForPlayerAction = false;
    }



    // ================= BASIC ATTACK =================

    public void SelectBasicAttack()
    {
        if (!waitingForPlayerAction)
            return;


        PlayerStatus player = currentUnit as PlayerStatus;


        if (player == null)
            return;


        EnemyStatus target = GetEnemyTarget();


        if (target == null)
        {
            EndPlayerAction();
            return;
        }


        var attack = player.BasicAttack.CreateInstance();

        attack.Use(player, target);

        EndPlayerAction();
    }



    // ================= SKILL =================

    public void UseSkill(int index)
    {
        if (!waitingForPlayerAction)
            return;


        PlayerStatus player = currentUnit as PlayerStatus;


        if (player == null)
            return;


        EnemyStatus target = GetEnemyTarget();


        if (target == null)
        {
            EndPlayerAction();
            return;
        }


        var skill = player.GetSkillByIndex(index);


        if (skill == null)
        {
            EndPlayerAction();
            return;
        }


        var attack = skill.CreateInstance();

        attack.Use(player, target);

        EndPlayerAction();
    }



    // ================= ENEMY TURN =================

    private IEnumerator EnemyTurn()
    {

        PlayerStatus target = GetAlivePlayer();


        if (target == null)
            yield break;


        EnemyStatus enemy = currentUnit as EnemyStatus;


        if (enemy == null)
            yield break;



        var attackData = enemy.GetRandomAttack();


        if (attackData != null)
        {
            Debug.Log(enemy.entityName + " attack");

            var attack = attackData.CreateInstance();

            attack.Use(enemy, target);
        }
        else
        {
            target.TakeDamage(enemy, enemy.Atk);
        }


        yield return new WaitForSeconds(0.5f);
    }



    // ================= TARGET =================

    private EnemyStatus GetEnemyTarget()
    {
        return enemyParty
            .GetMemberBySlot(currentUnit.LastTargetSlot)
            as EnemyStatus;
    }



    public PlayerStatus GetAlivePlayer()
    {
        foreach (Status s in playerParty.Members)
        {
            PlayerStatus p = s as PlayerStatus;

            if (p != null && p.IsAlive)
                return p;
        }

        return null;
    }

    // ================= TARGET INPUT =================

    public void ChangeTargetInput(int direction)
    {
        if (currentUnit == null)
            return;

        int current = currentUnit.LastTargetSlot;

        int next = GetNextAliveEnemySlot(current, direction);

        currentUnit.LastTargetSlot = next;

        EnemyStatus target =
            enemyParty.GetMemberBySlot(next) as EnemyStatus;

        if (target != null)
        {
            Debug.Log("Target changed to: " + target.entityName);
        }
    }



    private int GetNextAliveEnemySlot(int start, int direction)
    {
        int count = enemyParty.Members.Count;

        int index = start;

        for (int i = 0; i < count; i++)
        {
            index = (index + direction + count) % count;

            EnemyStatus e =
                enemyParty.GetMemberBySlot(index) as EnemyStatus;

            if (e != null && e.IsAlive)
                return index;
        }

        return start;
    }

    private void RetargetIfDead()
    {
        for (int i = 0; i < enemyParty.Members.Count; i++)
        {
            EnemyStatus e =
                enemyParty.GetMemberBySlot(i) as EnemyStatus;

            if (e != null && e.IsAlive)
            {
                currentUnit.LastTargetSlot = i;
                return;
            }
        }
    }



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