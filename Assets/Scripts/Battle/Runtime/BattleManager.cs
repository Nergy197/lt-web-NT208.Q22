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

    // ================= START =================

    IEnumerator Start()
    {
        Debug.Log("[BATTLE] Start");

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

            Debug.Log("[BATTLE] Spawn enemy: " + e.entityName);
        }
    }

    // ================= INIT =================

    void InitBattle()
    {
        timeline.Clear();

        AddParty(playerParty);

        AddParty(enemyParty);

        StartCoroutine(BattleLoop());
    }

    void AddParty(Party party)
    {
        foreach (var s in party.Members)
        {
            s.ResetForBattle(BASE_DELAY);

            timeline.Add(s);
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

            Debug.Log("[TURN] " + currentUnit.entityName);

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

        Debug.Log("[TURN] Player waiting");

        yield return new WaitUntil(() =>
        waitingForPlayerAction == false);
    }

    public void SelectBasicAttack()
    {
        var player = currentUnit as PlayerStatus;

        var enemy = GetEnemyTarget();

        if (enemy == null)
        {
            Debug.LogWarning("[BATTLE] No valid enemy target for basic attack");
            waitingForPlayerAction = false;
            return;
        }

        Debug.Log("[ACTION] Attack " + enemy.entityName);

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
            Debug.LogWarning("[BATTLE] No valid enemy target for skill");
            waitingForPlayerAction = false;
            return;
        }

        var skill = player.GetSkillByIndex(index);

        if (skill == null)
        {
            Debug.LogWarning($"[BATTLE] Skill at index {index} not found for {player.entityName}");
            waitingForPlayerAction = false;
            return;
        }

        skill
        .CreateInstance()
        .Use(player, enemy);

        waitingForPlayerAction = false;
    }

    public void RequestParry()
    {
        var player = currentUnit as PlayerStatus;

        player?.RequestParry();
    }

    // ================= ENEMY =================

    IEnumerator EnemyTurn()
    {
        var enemy = currentUnit as EnemyStatus;

        var player = GetAlivePlayer();

        Debug.Log("[ENEMY] Attack " + player.entityName);

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
        Debug.Log("[TARGET] change " + dir);
    }

    // ================= TIMELINE =================

    Status GetNextUnit()
    {
        timeline.Sort((a, b) =>
        a.NextTurnTime.CompareTo(b.NextTurnTime));

        var unit = timeline[0];

        unit.NextTurnTime +=
        BASE_DELAY / unit.Spd;

        return unit;
    }

    // ================= END =================

    bool CheckEndBattle()
    {
        if (enemyParty.IsDefeated())
        {
            Debug.Log("[BATTLE] WIN");
            return true;
        }

        if (playerParty.IsDefeated())
        {
            Debug.Log("[BATTLE] LOSE");
            return true;
        }

        return false;
    }

    void EndBattle()
    {
        Debug.Log("[BATTLE] End");

        GameManager.Instance.SavePlayerParty();

        InputController.Instance.UnbindBattleManager();

        MapManager.Instance.EndBattle();
    }

}