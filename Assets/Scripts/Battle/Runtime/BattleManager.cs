using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    private Party playerParty;
    private Party enemyParty;

    private List<Status> timeline = new();

    private Status currentUnit;

    private bool waitingForPlayerAction;

    private const float BASE_DELAY = 100f;

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
        Log("[BATTLE] Start");

        yield return new WaitUntil(() =>
            GameManager.Instance != null &&
            GameManager.Instance.isLoaded);

        playerParty = GameManager.Instance.GetPlayerParty();

        LoadEnemy();

        InputController.Instance.BindBattleManager(this);

        InitBattle();
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

            if (currentUnit is PlayerStatus)
                yield return PlayerTurn();
            else
                yield return EnemyTurn();
        }

        EndBattle();
    }

    // ================= PLAYER =================

    IEnumerator PlayerTurn()
    {
        waitingForPlayerAction = true;

        Log("[TURN] Waiting Player Action");

        yield return new WaitUntil(() =>
            waitingForPlayerAction == false);
    }

    public void SelectBasicAttack()
    {
        var player = currentUnit as PlayerStatus;

        var enemy = GetEnemyTarget();

        if (enemy == null)
        {
            Log("[ERROR] No enemy target");

            waitingForPlayerAction = false;
            return;
        }

        Log("[ACTION] Attack → " + enemy.entityName);

        player.BasicAttack
            .CreateInstance()
            .Use(player, enemy);

        waitingForPlayerAction = false;
    }

    public void UseSkill(int index)
    {
        var player = currentUnit as PlayerStatus;

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
            Log("[ERROR] Skill not found");

            waitingForPlayerAction = false;
            return;
        }

        Log("[ACTION] Skill → " + skill.name);

        skill.CreateInstance()
            .Use(player, enemy);

        waitingForPlayerAction = false;
    }

    public void RequestParry()
    {
        var player = currentUnit as PlayerStatus;

        player?.RequestParry();

        Log("[ACTION] Parry");
    }

    // ================= ENEMY =================

    IEnumerator EnemyTurn()
    {
        var enemy = currentUnit as EnemyStatus;

        var player = GetAlivePlayer();

        Log("[ENEMY] Attack → " + player.entityName);

        enemy.GetRandomAttack()
            .CreateInstance()
            .Use(enemy, player);

        yield return new WaitForSeconds(1f);
    }

    // ================= TARGET =================

    EnemyStatus GetEnemyTarget()
    {
        foreach (var e in enemyParty.Members)
            if (e.IsAlive)
                return e as EnemyStatus;

        return null;
    }

    PlayerStatus GetAlivePlayer()
    {
        foreach (var p in playerParty.Members)
            if (p.IsAlive)
                return p as PlayerStatus;

        return null;
    }

    public void ChangeTargetInput(int dir)
    {
        Log("[TARGET] Change → " + dir);
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
        if (enemyParty.IsDefeated())
        {
            Log("[BATTLE] WIN");
            return true;
        }

        if (playerParty.IsDefeated())
        {
            Log("[BATTLE] LOSE");
            return true;
        }

        return false;
    }

    void EndBattle()
    {
        Log("[BATTLE] End");

        GameManager.Instance.SavePlayerParty();

        InputController.Instance.UnbindBattleManager();

        MapManager.Instance.EndBattle();
    }
}