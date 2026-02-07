using UnityEngine;

[System.Serializable]
public abstract class Status
{
    public string entityName;
    public int level = 1;

    // BASE STATS
    protected int baseHP;
    protected int baseAtk;
    protected int baseDef;
    protected int baseSpd;

    // GROWTH STATS
    protected int hpGrowth;
    protected int atkGrowth;
    protected int defGrowth;
    protected int spdGrowth;

    // REAL-TIME CALCULATION
    public int MaxHP => baseHP + (hpGrowth * (level - 1));
    public int Atk   => baseAtk + (atkGrowth * (level - 1));
    public int Def   => baseDef + (defGrowth * (level - 1));
    public int Spd   => baseSpd + (spdGrowth * (level - 1));

    // DYNAMIC STATE
    public int currentHP;
    public bool IsAlive => currentHP > 0;

    // CONSTRUCTOR
    public Status(string name, int hp, int atk, int def, int spd)
    {
        entityName = name;
        baseHP = hp;
        baseAtk = atk;
        baseDef = def;
        baseSpd = spd;

        // Tự động tính chỉ số tăng trưởng
        hpGrowth = Mathf.RoundToInt(hp * 0.2f);   
        atkGrowth = Mathf.Max(1, Mathf.RoundToInt(atk * 0.15f));
        defGrowth = Mathf.Max(1, Mathf.RoundToInt(def * 0.1f));
        spdGrowth = 1; 

        currentHP = MaxHP;
    }

    public virtual void SetLevel(int targetLevel)
    {
        if (targetLevel < 1) return;  // Safety: level must be >= 1
        
        // Only update if level actually changes, prevent resurrection on duplicate calls
        if (this.level == targetLevel) return;
        
        this.level = targetLevel;
        HealFull();
        Debug.Log($"{entityName} set to level {level}, HP restored to {MaxHP}");
    }

    public void TakeDamage(Status attacker, int rawDamage)
    {
        if (attacker == null || !attacker.IsAlive)
            return;

        if (!IsAlive)
            return;

        int finalDmg = Mathf.Max(1, rawDamage - Def);
        currentHP -= finalDmg;
        if (currentHP < 0) currentHP = 0;
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > MaxHP) currentHP = MaxHP;
    }

    public void HealFull() => currentHP = MaxHP;
}