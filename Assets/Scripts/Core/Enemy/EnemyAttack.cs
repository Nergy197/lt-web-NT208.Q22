public class EnemyAttack : AttackBase
{
    public EnemyAttack() : base("Enemy Attack") { }

    public override void Use(Status attacker, Status target)
    {
        EnemyStatus enemy = attacker as EnemyStatus;
        PlayerStatus player = target as PlayerStatus;

        if (enemy == null || player == null)
            return;

        player.TakeDamage(enemy.Atk);
    }
}
