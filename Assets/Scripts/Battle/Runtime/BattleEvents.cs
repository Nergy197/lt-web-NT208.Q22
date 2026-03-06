using System;

public static class BattleEvents
{
    // Giữ nguyên để tương thích với BattleManager.waitingForAttackFinish
    public static event Action OnAttackFinished;

    public static void RaiseAttackFinished()
    {
        OnAttackFinished?.Invoke();

        // Đồng thời phát qua EventManager để các subscriber toàn game nhận được
        EventManager.Publish(GameEvent.AttackFinished);
    }
}
