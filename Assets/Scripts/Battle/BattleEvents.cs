using System;

public static class BattleEvents
{
    public static event Action OnAttackFinished;

    public static void RaiseAttackFinished()
    {
        OnAttackFinished?.Invoke();
    }
}
