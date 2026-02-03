public class BasicAttack : AttackBase
{
    public BasicAttack() : base("Basic Attack") { }

    public override void Use(Status attacker, Status target)
    {
        // Type checking
        if (!(attacker is PlayerStatus player))
            return;
        if (!(target is EnemyStatus enemy))
            return;

        if (!player.IsAlive || !enemy.IsAlive)
            return;

        enemy.TakeDamage(player, player.Atk);

        // đánh thường → +1 AP
        player.GainAP(1);
    }
}
