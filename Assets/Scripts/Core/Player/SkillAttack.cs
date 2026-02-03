public abstract class PlayerAttack : AttackBase
{
    public int apCost;

    protected PlayerAttack(string name, int apCost)
        : base(name)
    {
        this.apCost = apCost;
    }

    public override void Use(Character attacker, Character target)
    {
        // chỉ player mới được dùng
        if (attacker.Side != CharacterSide.Player)
            return;

        if (!attacker.CanUseAP(apCost))
            return;

        attacker.UseAP(apCost);

        // PLAYER CÓ THỂ PARRY =====
        if (target.TryParry(attacker))
        {
            OnParried(attacker, target);
            return;
        }

        Execute(attacker, target);
        AfterExecute(attacker);
    }

    protected abstract void Execute(Character attacker, Character target);

    protected virtual void AfterExecute(Character attacker) { }

    protected virtual void OnParried(Character attacker, Character defender)
    {
        // counter cơ bản
        attacker.TakeDamage(defender.atk / 2);
    }
}
