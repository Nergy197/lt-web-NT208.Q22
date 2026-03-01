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

    private const float BASE_DELAY = 100f;



    // ================= START =================

    private IEnumerator Start()
    {
        yield return WaitForPlayerParty();

        LoadEnemyFromMap();

        // Bind Input
        InputController.Instance.BindBattleManager(this);

        InitBattle();
    }



    // ================= WAIT PLAYER PARTY =================

    private IEnumerator WaitForPlayerParty()
    {
        while (GameManager.Instance == null ||
               GameManager.Instance.GetPlayerParty() == null)
        {
            yield return null;
        }

        playerParty =
            GameManager.Instance.GetPlayerParty();

        Debug.Log("Player Party Ready");
    }



    // ================= LOAD ENEMY =================

    private void LoadEnemyFromMap()
    {
        enemyParty =
            new Party(PartyType.Enemy);


        var list =
            MapManager.Instance.currentEnemies;


        int level =
            MapManager.Instance.currentMapLevel;


        Debug.Log("Spawn enemy count: " + list.Count);


        foreach (var enemyData in list)
        {
            if (enemyData == null)
                continue;


            EnemyStatus enemy =
                enemyData.CreateStatus();


            enemy.SetLevel(level);


            // áp dụng buff map nếu có
            MapManager.Instance
                .ApplyEnemyEffects(enemy);


            enemyParty.AddMember(enemy);


            Debug.Log(
            "Spawn: " +
            enemy.entityName);
        }
    }



    // ================= INIT =================

    private void InitBattle()
    {
        timeline.Clear();

        AddParty(playerParty);

        AddParty(enemyParty);

        StartCoroutine(BattleLoop());
    }



    private void AddParty(Party party)
    {
        foreach (Status s in party.Members)
        {
            s.ResetForBattle(BASE_DELAY);

            timeline.Add(s);
        }
    }



    // ================= MAIN LOOP =================

    private IEnumerator BattleLoop()
    {
        while (true)
        {
            if (CheckEndBattle())
                break;


            currentUnit =
                GetNextUnit();


            if (currentUnit.PartyType ==
                PartyType.Player)

                yield return PlayerTurn();

            else

                yield return EnemyTurn();
        }


        EndBattle();
    }



    // ================= PLAYER TURN =================

    private IEnumerator PlayerTurn()
    {
        waitingForPlayerAction = true;

        Debug.Log("Player Turn");

        yield return new WaitUntil(() =>
            waitingForPlayerAction == false);
    }



    public void SelectBasicAttack()
    {
        if (!waitingForPlayerAction)
            return;


        PlayerStatus player =
            currentUnit as PlayerStatus;


        EnemyStatus enemy =
            GetEnemyTarget();


        var atk =
            player.BasicAttack
            .CreateInstance();


        atk.Use(player, enemy);


        waitingForPlayerAction = false;
    }



    public void UseSkill(int index)
    {
        if (!waitingForPlayerAction)
            return;


        PlayerStatus player =
            currentUnit as PlayerStatus;


        EnemyStatus enemy =
            GetEnemyTarget();


        var skill =
            player.GetSkillByIndex(index);


        if (skill == null)
        {
            waitingForPlayerAction = false;
            return;
        }


        var atk =
            skill.CreateInstance();


        atk.Use(player, enemy);


        waitingForPlayerAction = false;
    }



    // ================= ENEMY TURN =================

    private IEnumerator EnemyTurn()
    {
        EnemyStatus enemy =
            currentUnit as EnemyStatus;


        PlayerStatus player =
            GetAlivePlayer();


        var atk =
            enemy.GetRandomAttack()
            .CreateInstance();


        atk.Use(enemy, player);


        yield return
            new WaitForSeconds(0.5f);
    }



    // ================= TARGET =================

    public void ChangeTargetInput(int direction)
    {
        if (currentUnit == null)
            return;


        int current =
            currentUnit.LastTargetSlot;


        int count =
            enemyParty.Members.Count;


        int next =
            (current + direction + count)
            % count;


        currentUnit.LastTargetSlot =
            next;


        Debug.Log("Target slot: " + next);
    }



    private EnemyStatus GetEnemyTarget()
    {
        return enemyParty
            .GetMemberBySlot(
            currentUnit.LastTargetSlot)
            as EnemyStatus;
    }



    public PlayerStatus GetAlivePlayer()
    {
        foreach (Status s in playerParty.Members)
        {
            if (s is PlayerStatus p &&
                p.IsAlive)
                return p;
        }

        return null;
    }



    // ================= TIMELINE =================

    private Status GetNextUnit()
    {
        timeline.Sort((a, b) =>
            a.NextTurnTime
            .CompareTo(b.NextTurnTime));


        Status unit =
            timeline[0];


        unit.NextTurnTime +=
            BASE_DELAY /
            Mathf.Max(1, unit.Spd);


        return unit;
    }



    // ================= END =================

    private bool CheckEndBattle()
    {
        if (enemyParty.IsDefeated())
        {
            Debug.Log("PLAYER WIN");

            return true;
        }


        if (playerParty.IsDefeated())
        {
            Debug.Log("PLAYER LOSE");

            return true;
        }


        return false;
    }

    private void EndBattle()
    {
        Debug.Log("Battle End");


        GameManager.Instance
        .SavePlayerParty();


        InputController.Instance
        .UnbindBattleManager();


        MapManager.Instance
        .EndBattle();
    }

}