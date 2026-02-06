using UnityEngine;
using System.Collections;
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

    // Turn flow control
    private bool isRunning = false;                 // prevent multiple coroutines
    private bool waitingForPlayerAction = false;    // when true, coroutine waits for player's input

    // ================= INIT =================
    public void InitBattle(Party player, Party enemy, int mapLevel)
    {
        playerParty = player;
        enemyParty = enemy;

        foreach (var e in enemyParty.Members.OfType<EnemyStatus>())
            e.SetLevel(mapLevel);

        // build turn order (simple: theo thứ tự add)
        turnOrder.Clear();
        foreach (var s in playerParty.Members.Concat(enemyParty.Members))
            turnOrder.Enqueue(s);

        State = BattleState.Start;
        turnCount = 0;
        currentUnit = null;
        targetIndex = 0;

        Debug.Log("=== BATTLE START ===");

        // start the main loop (only one coroutine will run)
        if (!isRunning)
            StartCoroutine(BattleLoop());
    }

    // ================= MAIN LOOP (Coroutine) =================
    private IEnumerator BattleLoop()
    {
        isRunning = true;

        // Main loop: chạy cho tới khi CheckEndBattle() trả true
        while (!CheckEndBattle())
        {
            // find next alive unit in queue with safety
            if (turnOrder.Count == 0)
            {
                Debug.LogWarning("Turn order is empty. Exiting battle loop.");
                break;
            }

            int safety = Mathf.Max(1, turnOrder.Count); // an toàn: chỉ thử tối đa Count lần
            Status next = null;
            for (int i = 0; i < safety; i++)
            {
                var s = turnOrder.Dequeue();
                turnOrder.Enqueue(s);
                if (s.IsAlive)
                {
                    next = s;
                    break;
                }
            }

            if (next == null)
            {
                // không tìm thấy unit sống trong 1 vòng queue
                Debug.LogWarning("No alive unit found in turn order. Rechecking end conditions...");
                // nếu party thực sự không defeated, break to avoid infinite loop
                if (playerParty.IsDefeated() || enemyParty.IsDefeated())
                    break;
                else
                {
                    Debug.LogWarning("Neither party is defeated but no alive unit found — aborting loop.");
                    break;
                }
            }

            currentUnit = next;
            turnCount++;

            State = currentUnit is PlayerStatus ? BattleState.PlayerTurn : BattleState.EnemyTurn;

            Debug.Log($"================ TURN {turnCount} ================");
            Debug.Log($"--> {currentUnit.entityName} TURN ({State})");

            LogBattleState();

            // PLAYER TURN: wait for player input (SelectBasicAttack, skills, items...).
            if (State == BattleState.PlayerTurn)
            {
                // If the current unit is not alive for some reason (rare), skip
                if (!currentUnit.IsAlive)
                {
                    Debug.Log($"Skipping dead player {currentUnit.entityName}");
                    yield return null;
                    continue;
                }

                // set flag and wait until player acts
                waitingForPlayerAction = true;

                // Wait until player finishes action OR battle ends
                yield return new WaitUntil(() => !waitingForPlayerAction || CheckEndBattle());

                // after player action returns control here
                // loop continues to next turn
            }
            else // ENEMY TURN: auto-act immediately
            {
                // double-check enemy alive
                if (!currentUnit.IsAlive)
                {
                    Debug.Log($"Skipping dead enemy {currentUnit.entityName}");
                    yield return null;
                    continue;
                }

                EnemyAutoAttack(); // does damage synchronously
                // small frame delay so logs are readable and other systems update
                yield return null;
            }

            // allow one frame to update UI / systems before next turn
            yield return null;
        }

        // finished battle
        isRunning = false;
        Debug.Log("Battle loop ended.");
    }

    // ================= PLAYER =================
    public void SelectBasicAttack()
    {
        // Only allow when it's player's turn and currentUnit is the player
        if (State != BattleState.PlayerTurn) return;
        if (!(currentUnit is PlayerStatus)) return;
        if (!currentUnit.IsAlive) // safety: if player died while waiting
        {
            waitingForPlayerAction = false; // resume loop (it will skip)
            return;
        }

        var target = GetCurrentTargetEnemy();
        if (target == null)
        {
            Debug.Log("No valid target to attack.");
            waitingForPlayerAction = false;
            return;
        }

        Debug.Log($"PLAYER {currentUnit.entityName} attacks {target.entityName}");
        target.TakeDamage(currentUnit, currentUnit.Atk);

        // After doing the action, clear waiting flag so coroutine continues to next turn
        waitingForPlayerAction = false;
    }

    public void ChangeTargetInput(int dir)
    {
        if (State != BattleState.PlayerTurn) return;

        var enemies = enemyParty.Members.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0)
        {
            targetIndex = 0;
            return;
        }

        // ensure targetIndex valid and stable across deaths
        targetIndex = (targetIndex + dir) % enemies.Count;
        if (targetIndex < 0) targetIndex += enemies.Count;

        Debug.Log($"Target -> {enemies[targetIndex].entityName}");
    }

    private EnemyStatus GetCurrentTargetEnemy()
    {
        var enemies = enemyParty.Members.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return null;

        // clamp targetIndex to current list
        targetIndex = Mathf.Clamp(targetIndex, 0, enemies.Count - 1);
        return enemies[targetIndex] as EnemyStatus;
    }

    // ================= ENEMY =================
    private void EnemyAutoAttack()
    {
        // ensure enemy still alive
        if (currentUnit == null || !currentUnit.IsAlive)
        {
            Debug.Log("EnemyAutoAttack called but currentUnit is null or dead.");
            return;
        }

        var target = playerParty.Members
            .OfType<PlayerStatus>()
            .FirstOrDefault(p => p.IsAlive);

        if (target == null) return;

        Debug.Log($"ENEMY {currentUnit.entityName} attacks {target.entityName}");
        target.TakeDamage(currentUnit, currentUnit.Atk);
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
        if (enemyParty == null || playerParty == null)
            return true;

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
