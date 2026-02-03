public class BasicAttack : PlayerAttack
{
    public BasicAttack() : base("Attack", 0) { }

    protected override void Execute(Character attacker, Character target)
    {
        target.TakeDamage(attacker.atk);
    }

    protected override void AfterExecute(Character attacker)
    {
        attacker.GainAP(1); // đánh thường để tích AP
    }
}
