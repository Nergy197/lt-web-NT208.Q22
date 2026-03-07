using System;

/// <summary>
/// Event bus riêng cho battle — tách khỏi EventManager để các subscriber ngoài
/// scene (như BattleManager) có thể lắng nghe trực tiếp không qua payload boxing.
/// </summary>
public static class BattleEvents
{
    public static event Action OnAttackFinished;

    /// <summary>
    /// Gọi khi một đòn tấn công hoàn thành toàn bộ phases (Prepare → Execute → Recovery).
    /// Đồng thời phát qua EventManager để các module khác có thể phản응 nếu cần.
    /// </summary>
    public static void RaiseAttackFinished()
    {
        OnAttackFinished?.Invoke();
        EventManager.Publish(GameEvent.AttackFinished);
    }
}
