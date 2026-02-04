using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    // ===== STATE =====
    public BattleState State { get; private set; }

    // ===== CORE DATA =====
    private Party playerParty;
    private Party enemyParty;

    private TurnManager turnManager = new TurnManager();
    private Queue<Status> turnOrder = new Queue<Status>();
    private Status currentUnit;

    // [THÊM MỚI] Biến để theo dõi người chơi đang nhắm vào ai
    private int targetIndex = 0; 
    private bool isActionSelected = false;
    private AttackBase currentSelectedAttack = null;
    
    // [SỬA LẠI] Thêm [SerializeField] để hiện ra ngoài Inspector
    [SerializeField] private InputConfig keymap;
    // ===== PARRY SYSTEM (MODEL 3 - Real-time window) =====
    private bool isParryWindowOpen = false;
    private float parryWindowDuration = 2.5f; // 2.5 seconds
    private float parryWindowStartTime = 0f;
    private EnemyStatus currentAttacker = null;
    private PlayerStatus parryTarget = null;

    // ===== INIT BATTLE =====
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
        turnOrder = new Queue<Status>(turnManager.BuildTurnOrder(playerParty, enemyParty));
        currentUnit = turnOrder.Count > 0 ? turnOrder.Peek() : null;

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

    // ===== PARRY SYSTEM (MODEL 3) =====
    // ===== UPDATE LOOP =====
    private void Update()
    {
        // Safety check
        if (keymap == null) return;

        // 1. ƯU TIÊN CAO NHẤT: PARRY (Có thể bấm bất cứ lúc nào nếu cửa sổ đang mở)
        if (isParryWindowOpen)
        {
            // Kiểm tra hết giờ
            if (Time.time - parryWindowStartTime >= parryWindowDuration)
            {
                OnParryWindowExpired();
                return;
            }

            // Kiểm tra nút bấm Parry
            if (Input.GetKeyDown(keymap.parryKey))
            {
                PlayerAttemptParry();
                return; // Đã parry xong thì không làm việc khác
            }
        }

        // 2. LƯỢT CỦA NGƯỜI CHƠI (Chỉ điều khiển khi đến lượt và không phải đang đỡ đòn)
        if (State == BattleState.PlayerTurn && !isParryWindowOpen)
        {
            HandlePlayerInput();
        }
    }

    private void HandlePlayerInput()
    {
        // --- A. ĐIỀU HƯỚNG MỤC TIÊU ---
        if (Input.GetKeyDown(keymap.nextTargetKey))
        {
            ChangeTarget(1); // Qua phải
        }
        else if (Input.GetKeyDown(keymap.prevTargetKey))
        {
            ChangeTarget(-1); // Qua trái
        }

        // --- B. CHỌN HÀNH ĐỘNG ---
        if (Input.GetKeyDown(keymap.basicAttackKey))
        {
            Debug.Log("Đã chọn: Đánh thường (Tốn 2 AP)");
            // Giả lập tạo đòn đánh (Cần khớp constructor class PlayerAttack của bạn)
            // currentSelectedAttack = new PlayerAttack("Basic", 2, new List<PlayerAttackHit>()); 
            isActionSelected = true;
        }
        else if (Input.GetKeyDown(keymap.skill1Key))
        {
            Debug.Log("Đã chọn: Skill 1 (Tốn 5 AP)");
            // currentSelectedAttack = new PlayerAttack("Skill Fire", 5, ...);
            isActionSelected = true;
        }

        // --- C. XÁC NHẬN ---
        if (Input.GetKeyDown(keymap.confirmKey))
        {
            if (isActionSelected)
            {
                // Lấy mục tiêu hiện tại
                var target = GetCurrentTargetEnemy();
                if (target != null)
                {
                    Debug.Log($"Tấn công {target.entityName}!");
                    // Thực thi đòn đánh (Bỏ comment khi bạn đã cài logic Attack xong)
                    // ExecuteAttack(currentSelectedAttack, target);
                    
                    // Reset trạng thái
                    isActionSelected = false; 
                }
            }
            else
            {
                Debug.Log("Chưa chọn hành động nào! Hãy bấm E hoặc Q trước.");
            }
        }
        
        // --- D. HỦY BỎ ---
        if (Input.GetKeyDown(keymap.cancelKey))
        {
            isActionSelected = false;
            currentSelectedAttack = null;
            Debug.Log("Đã hủy chọn.");
        }
    }

    // ===== CÁC HÀM HỖ TRỢ LOGIC MỚI =====

    // Hàm đổi mục tiêu xoay vòng
    private void ChangeTarget(int direction)
    {
        // Lấy danh sách kẻ địch còn sống
        var liveEnemies = enemyParty.Members.Where(e => e.IsAlive).ToList();
        if (liveEnemies.Count == 0) return;

        targetIndex += direction;

        // Xử lý xoay vòng (Loop)
        if (targetIndex >= liveEnemies.Count) targetIndex = 0;
        if (targetIndex < 0) targetIndex = liveEnemies.Count - 1;

        Debug.Log($"Đang nhắm vào: {liveEnemies[targetIndex].entityName}");
    }

    // Hàm lấy kẻ địch đang được nhắm
    private EnemyStatus GetCurrentTargetEnemy()
    {
        var liveEnemies = enemyParty.Members.Where(e => e.IsAlive).ToList();
        if (liveEnemies.Count == 0) return null;

        // Bảo vệ lỗi index
        if (targetIndex >= liveEnemies.Count) targetIndex = 0;

        return liveEnemies[targetIndex] as EnemyStatus;
    }

    public void OnEnemyAttackAnnounced(EnemyStatus attacker, PlayerStatus defender)
    {
        // Called when enemy announces attack
        // Opens parry window for 2.5 seconds
        isParryWindowOpen = true;
        parryWindowStartTime = Time.time;
        currentAttacker = attacker;
        parryTarget = defender;

        // UI should show countdown and "Parry!" button
        Debug.Log($"{attacker.entityName} is attacking! Parry window open for {parryWindowDuration}s");
    }

    public void PlayerAttemptParry()
    {
        if (!isParryWindowOpen || currentAttacker == null || parryTarget == null)
            return;

        float elapsedTime = Time.time - parryWindowStartTime;
        // Simple model: if player hits roughly the center of window -> guaranteed parry
        float perfectTime = parryWindowDuration / 2f;
        float timingDiff = Mathf.Abs(elapsedTime - perfectTime);

        const float perfectWindow = 0.5f; // ±0.5s around center => guaranteed parry
        bool parrySuccess;
        if (timingDiff <= perfectWindow)
        {
            parrySuccess = true;
        }
        else
        {
            parrySuccess = false;
        }
        if (parrySuccess)
        {
            Debug.Log($"{parryTarget.entityName} parried the attack!");
        }
        else
        {
            Debug.Log($"{parryTarget.entityName} failed to parry!");
            parryTarget.TakeDamage(currentAttacker, currentAttacker.Atk);
        }

        CloseParryWindow();
        EndTurn();
    }

    private void OnParryWindowExpired()
    {
        // Window closed - enemy attack lands
        if (parryTarget != null && currentAttacker != null)
        {
            Debug.Log($"{parryTarget.entityName} couldn't parry in time!");
            parryTarget.TakeDamage(currentAttacker, currentAttacker.Atk);
        }

        CloseParryWindow();
        EndTurn();
    }

    // Simplified: timing helper functions removed; perfect window used in PlayerAttemptParry

    private void CloseParryWindow()
    {
        isParryWindowOpen = false;
        currentAttacker = null;
        parryTarget = null;
    }

    public bool IsParryWindowOpen() => isParryWindowOpen;
    public float GetParryWindowProgress() => isParryWindowOpen ? 
        (Time.time - parryWindowStartTime) / parryWindowDuration : 0f;

    // ===== EXECUTE ACTION =====
    public bool ExecuteAttack(AttackBase attack, Status target)
    {
        if (State != BattleState.PlayerTurn && State != BattleState.EnemyTurn)
            return false;

        if (currentUnit == null || !currentUnit.IsAlive)
            return false;

        if (attack == null || target == null)
            return false;

        // If enemy attacks player - trigger parry window
        if (currentUnit is EnemyStatus enemy && target is PlayerStatus player)
        {
            OnEnemyAttackAnnounced(enemy, player);
            return true; // Wait for parry attempt or window expiry
        }

        // Normal attack execution
        attack.Use(currentUnit, target);

        EndTurn();
        return true;
    }

    // ===== END TURN =====
    private void EndTurn()
    {
        // Dequeue current unit
        if (turnOrder.Count > 0)
        {
            Status finishedUnit = turnOrder.Dequeue();

            // If still alive, enqueue back to end of queue
            if (finishedUnit.IsAlive)
            {
                turnOrder.Enqueue(finishedUnit);
            }
            else
            {
                // Unit died - handle rewards if it's an enemy
                if (finishedUnit is EnemyStatus defeatedEnemy)
                {
                    GiveRewardsForDefeatedEnemy(defeatedEnemy);
                }
            }
        }

        // Skip dead units at front of queue
        while (turnOrder.Count > 0 && !turnOrder.Peek().IsAlive)
        {
            Status deadUnit = turnOrder.Dequeue();
            if (deadUnit is EnemyStatus deadEnemy)
            {
                GiveRewardsForDefeatedEnemy(deadEnemy);
            }
        }

        // Get next unit
        currentUnit = turnOrder.Count > 0 ? turnOrder.Peek() : null;

        AdvanceState();
    }

    private void GiveRewardsForDefeatedEnemy(EnemyStatus defeatedEnemy)
    {
        int expReward = defeatedEnemy.GetExpReward();
        foreach (var member in playerParty.Members)
        {
            if (member is PlayerStatus player && player.IsAlive)
            {
                player.GainExp(expReward);
            }
        }
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
