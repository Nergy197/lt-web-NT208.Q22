public class EnemyAttack : AttackBase
{
    public EnemyAttack(string name = "Enemy Attack")
        : base(name) { }

    public override void Use(Character attacker, Character target)
    {
        // enemy đánh rất đơn giản
        if (attacker.Side != CharacterSide.Enemy)
            return;

        target.TakeDamage(attacker.atk);
    }
}
