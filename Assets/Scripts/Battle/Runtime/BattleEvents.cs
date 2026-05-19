using System;

/// <summary>
/// Event bus riêng cho battle — tách khỏi EventManager để các subscriber ngoài
/// scene (như BattleManager) có thể lắng nghe trực tiếp không qua payload boxing.
/// </summary>
public static class BattleEvents
{
    public static event Action OnAttackFinished;

    /// <summary>Fire ngay trước khi enemy bắt đầu thực hiện đòn đánh (sau plan, trước Execute).</summary>
    public static event Action<EnemyAttackData, EnemyStatus, PlayerStatus> OnEnemyAttackAnnounced;

    /// <summary>Fire ở đầu mỗi hit definition, trong giai đoạn wind-up — để hiển thị cảnh báo / parry hint.</summary>
    public static event Action<EnemyAttackHit, int, EnemyStatus> OnEnemyHitIncoming;

    /// <summary>Fire ngay khi player parry thành công (ConsumeParry trả về true).</summary>
    public static event Action<PlayerStatus> OnParrySuccess;

    /// <summary>Fire khi parry window mở — player bắt đầu có thể bấm parry.</summary>
    public static event Action<PlayerStatus> OnParryWindowOpened;

    // Tự động clear delegates khi Play mode bắt đầu (tránh stale callbacks)
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        OnAttackFinished       = null;
        OnEnemyAttackAnnounced = null;
        OnEnemyHitIncoming     = null;
        OnParrySuccess         = null;
        OnParryWindowOpened    = null;
    }

    /// <summary>
    /// Gọi khi một đòn tấn công hoàn thành toàn bộ phases (Prepare → Execute → Recovery).
    /// Đồng thời phát qua EventManager để các module khác có thể phản응 nếu cần.
    /// </summary>
    public static void RaiseAttackFinished()
    {
        OnAttackFinished?.Invoke();
        EventManager.Publish(GameEvent.AttackFinished);
    }

    public static void RaiseEnemyAttackAnnounced(EnemyAttackData attack, EnemyStatus enemy, PlayerStatus target)
        => OnEnemyAttackAnnounced?.Invoke(attack, enemy, target);

    public static void RaiseEnemyHitIncoming(EnemyAttackHit hit, int hitIndex, EnemyStatus enemy)
        => OnEnemyHitIncoming?.Invoke(hit, hitIndex, enemy);

    public static void RaiseParrySuccess(PlayerStatus player)
        => OnParrySuccess?.Invoke(player);

    public static void RaiseParryWindowOpened(PlayerStatus player)
        => OnParryWindowOpened?.Invoke(player);
}
