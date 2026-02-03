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
        // Type checking
        if (!(attacker is PlayerStatus player))
            return;
        if (!(target is EnemyStatus enemy))
            return;

        if (!player.IsAlive || !enemy.IsAlive)
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
