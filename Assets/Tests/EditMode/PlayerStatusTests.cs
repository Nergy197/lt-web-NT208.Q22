using NUnit.Framework;

public class PlayerStatusTests
{
    static PlayerStatus NewUnit() => new PlayerStatus("Hero", 100, 20, 10, 15);

    [Test]
    public void ExpToNextLevel_FollowsCurveFormula()
    {
        var u = NewUnit();
        Assert.AreEqual(100, u.expToNextLevel);   // Lv1: 100 * 1^1.5 = 100
        u.SetLevel(4);
        Assert.AreEqual(800, u.expToNextLevel);   // Lv4: 100 * 4^1.5 = 800
    }

    [Test]
    public void GainExp_LevelsUpAndKeepsRemainder()
    {
        var u = NewUnit();
        u.GainExp(250);                            // 250 - 100(Lv1) = 150; 150 < 283(Lv2) → dừng
        Assert.AreEqual(2, u.level);
        Assert.AreEqual(150, u.currentExp);
    }

    [Test]
    public void GainExp_CanLevelUpMultipleTimes()
    {
        var u = NewUnit();
        u.GainExp(100000);                         // dư sức lên nhiều cấp
        Assert.Greater(u.level, 2);
    }

    [Test]
    public void LevelUp_HealsFullAndRestoresAp()
    {
        var u = NewUnit();
        u.TakeDamage(null, 50);
        u.UseAP(0);                                // currentAP bắt đầu = 0
        u.GainExp(100);                            // đúng 1 cấp
        Assert.AreEqual(2, u.level);
        Assert.AreEqual(u.MaxHP, u.currentHP);     // hồi đầy khi lên cấp
        Assert.AreEqual(u.MaxAP, u.currentAP);     // hồi đầy AP khi lên cấp
    }

    [Test]
    public void Ap_CannotExceedMax()
    {
        var u = NewUnit();
        u.RestoreAP(1000);
        Assert.AreEqual(u.MaxAP, u.currentAP);
    }
}
