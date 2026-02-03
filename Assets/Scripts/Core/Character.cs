using System.Collections.Generic;

public enum CharacterSide
{
    Player,
    Enemy
}

public class Character
{
    public string name;
    public int maxHP;
    public int hp;
    public int atk;
    public int def;
    public int spd;

    public CharacterSide Side { get; private set; }

    public int maxAP = 3;
    public int ap = 0;

    // PLAYER ATTACK 
    public PlayerAttack BasicAttack { get; private set; }
    private List<PlayerAttack> equippedSkills = new List<PlayerAttack>();
    public IReadOnlyList<PlayerAttack> EquippedSkills => equippedSkills;

    // ENEMY ATTACK 
    public EnemyAttack EnemyAttack { get; private set; }

    // PARRY (PLAYER ONLY) 
    public bool IsParryReady { get; private set; }

    public bool IsAlive => hp > 0;

    public Character(
        string name,
        int hp,
        int atk,
        int def,
        int spd,
        CharacterSide side
    )
    {
        this.name = name;
        this.maxHP = hp;
        this.hp = hp;
        this.atk = atk;
        this.def = def;
        this.spd = spd;
        Side = side;

        if (side == CharacterSide.Player)
        {
            BasicAttack = new BasicAttack();
        }
        else
        {
            EnemyAttack = new EnemyAttack();
        }
    }

    // AP 
    public bool CanUseAP(int cost) => ap >= cost;

    public void UseAP(int cost) => ap = System.Math.Max(0, ap - cost);

    public void GainAP(int value) => ap = System.Math.Min(maxAP, ap + value);

    // PARRY 
    public void EnableParry()
    {
        if (Side == CharacterSide.Player)
            IsParryReady = true;
    }

    public bool TryParry(Character attacker)
    {
        if (Side != CharacterSide.Player)
            return false;

        if (!IsParryReady)
            return false;

        IsParryReady = false;
        return true;
    }

    // DAMAGE
    public void TakeDamage(int damage)
    {
        int final = damage - def;
        if (final < 1) final = 1;

        hp -= final;
        if (hp < 0) hp = 0;
    }
}
