using NUnit.Framework;

public class StatusTests
{
    // PlayerStatus("T", hp=100, atk=20, def=10, spd=15) → level 1: Atk=20, Def=10, MaxHP=100.
    static PlayerStatus NewUnit() => new PlayerStatus("T", 100, 20, 10, 15);

    [Test]
    public void TakeDamage_SubtractsDefWithMinimumOne()
    {
        var u = NewUnit();
        u.TakeDamage(null, 25);            // 25 - Def(10) = 15
        Assert.AreEqual(85, u.currentHP);

        u.TakeDamage(null, 5);             // 5 - 10 < 1 → tối thiểu 1
        Assert.AreEqual(84, u.currentHP);
    }

    [Test]
    public void TakeDamage_DiesAtZeroHp()
    {
        var u = NewUnit();
        u.TakeDamage(null, 1000);
        Assert.AreEqual(0, u.currentHP);
        Assert.IsFalse(u.IsAlive);
    }

    [Test]
    public void BuffAtk_AppliesThenRevertsOnExpire()
    {
        var u = NewUnit();
        int baseAtk = u.Atk;                                // 20
        u.ApplyStatusEffect(new StatusEffect("b", StatusEffectType.BuffAtk, 50, 2)); // +50% của 20 = +10
        Assert.AreEqual(baseAtk + 10, u.Atk);

        u.UpdateEffectDurations();                          // dur 2 → 1, vẫn còn
        Assert.AreEqual(baseAtk + 10, u.Atk);

        u.UpdateEffectDurations();                          // dur 1 → 0, hết hạn + undo
        Assert.AreEqual(baseAtk, u.Atk);
    }

    [Test]
    public void StackingBuffAtk_DoesNotCompound()
    {
        var u = NewUnit();
        int baseAtk = u.Atk;                                // 20
        // Mỗi buff tính % trên giá trị NỀN (20), không phải trên giá trị đã buff.
        u.ApplyStatusEffect(new StatusEffect("b1", StatusEffectType.BuffAtk, 50, 5));
        u.ApplyStatusEffect(new StatusEffect("b2", StatusEffectType.BuffAtk, 50, 5));
        Assert.AreEqual(baseAtk + 20, u.Atk);               // 20 + 10 + 10, KHÔNG phải 45
    }

    [Test]
    public void DebuffDef_RevertsExactlyOnExpire()
    {
        var u = NewUnit();
        int baseDef = u.Def;                                // 10
        u.ApplyStatusEffect(new StatusEffect("d", StatusEffectType.DebuffDef, 50, 1)); // -5
        Assert.AreEqual(baseDef - 5, u.Def);
        u.UpdateEffectDurations();
        Assert.AreEqual(baseDef, u.Def);
    }

    [Test]
    public void BuffHP_DoesNotLeaveCurrentHpAboveMaxAfterExpire()
    {
        var u = NewUnit();
        int baseMax = u.MaxHP;                              // 100
        u.ApplyStatusEffect(new StatusEffect("hp", StatusEffectType.BuffHP, 50, 1)); // MaxHP → 150
        Assert.AreEqual(baseMax + 50, u.MaxHP);
        u.HealFull();
        Assert.AreEqual(baseMax + 50, u.currentHP);         // 150

        u.UpdateEffectDurations();                          // hết hạn → MaxHP về 100
        Assert.AreEqual(baseMax, u.MaxHP);
        Assert.LessOrEqual(u.currentHP, u.MaxHP);           // currentHP bị kẹp về 100
        Assert.AreEqual(baseMax, u.currentHP);
    }

    [Test]
    public void ClearAllEffects_ResetsStatsToBase()
    {
        var u = NewUnit();
        int baseAtk = u.Atk;
        u.ApplyStatusEffect(new StatusEffect("b", StatusEffectType.BuffAtk, 50, 5));
        u.ApplyStatusEffect(new StatusEffect("d", StatusEffectType.DebuffSpd, 50, 5));
        u.ClearAllEffects();
        Assert.AreEqual(baseAtk, u.Atk);
    }
}
