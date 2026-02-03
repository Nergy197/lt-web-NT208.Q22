public class EnemyAttack : AttackBase
{
    public EnemyAttack() : base("Enemy Attack") { }

    public override void Use(Status attacker, Status target)
    {
        if (!(attacker is EnemyStatus enemy))
            return;
        if (!(target is PlayerStatus player))
            return;

        if (!enemy.IsAlive || !player.IsAlive)
            return;

        player.TakeDamage(enemy, enemy.Atk);
    }
}
