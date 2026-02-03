public class BasicAttack : AttackBase
{
    public BasicAttack() : base("Basic Attack") { }

    public override void Use(Status attacker, Status target)
    {
        PlayerStatus player = attacker as PlayerStatus;
        EnemyStatus enemy = target as EnemyStatus;

        if (player == null || enemy == null)
            return;

        enemy.TakeDamage(player.Atk);

        // đánh thường → +1 AP
        player.GainAP(1);
    }
}
