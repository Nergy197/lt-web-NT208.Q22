public abstract class AttackBase
{
    public string Name;

    protected AttackBase(string name)
    {
        Name = name;
    }

    public abstract void Use(Character attacker, Character target);
}
