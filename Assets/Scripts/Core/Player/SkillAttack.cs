public abstract class PlayerAttack : AttackBase
{
    public int apCost;

    protected PlayerAttack(string name, int apCost)
        : base(name)
    {
        this.apCost = apCost;
    }

    public override void Use(Status attacker, Status target)
    {
        PlayerStatus player = attacker as PlayerStatus;
        EnemyStatus enemy = target as EnemyStatus;

        // chỉ player mới dùng được
        if (player == null || enemy == null)
            return;

        // check AP
        if (!player.CanUseAP(apCost))
            return;

        player.UseAP(apCost);

        // ===== PARRY (PLAYER CHỈ PARRY KHI BỊ ĐÁNH, KHÔNG PHẢI Ở ĐÂY) =====
        // PlayerAttack KHÔNG xử lý parry của enemy

        Execute(player, enemy);
        AfterExecute(player);
    }

    protected abstract void Execute(PlayerStatus player, EnemyStatus enemy);

    protected virtual void AfterExecute(PlayerStatus player) { }
}
