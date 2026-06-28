using NUnit.Framework;

public class BattleTargetingTests
{
    static EnemyStatus E(string n) => new EnemyStatus(n, 50, 10, 5, 5);
    static PlayerStatus P(string n, int hp = 100) => new PlayerStatus(n, hp, 20, 10, 15);

    static (BattleTargeting t, Party players, Party enemies) Setup()
    {
        var players = new Party(PartyType.Player);
        var enemies = new Party(PartyType.Enemy);
        var t = new BattleTargeting();
        t.SetParties(players, enemies);
        return (t, players, enemies);
    }

    [Test]
    public void GetEnemyTarget_ReturnsNullWhenNoEnemies()
    {
        var (t, _, _) = Setup();
        Assert.IsNull(t.GetEnemyTarget());
    }

    [Test]
    public void GetEnemyTarget_ClampsIndexWhenOutOfRange()
    {
        var (t, _, enemies) = Setup();
        enemies.AddMember(E("e0"));
        enemies.AddMember(E("e1"));
        t.EnemyIndex = 99;                       // ngoài phạm vi
        Assert.AreEqual("e1", t.GetEnemyTarget().entityName); // clamp về index cuối
    }

    [Test]
    public void CycleEnemy_WrapsAround()
    {
        var (t, _, enemies) = Setup();
        enemies.AddMember(E("e0"));
        enemies.AddMember(E("e1"));
        t.EnemyIndex = 0;
        Assert.AreEqual("e1", t.CycleEnemy(+1).entityName);
        Assert.AreEqual("e0", t.CycleEnemy(+1).entityName); // wrap 1 → 0
        Assert.AreEqual("e1", t.CycleEnemy(-1).entityName); // wrap 0 → 1
    }

    [Test]
    public void IndexOfLowestHpAlly_HandlesEmptyAndSingle()
    {
        var (t, players, _) = Setup();
        Assert.AreEqual(0, t.IndexOfLowestHpAlly());   // không có ally → 0 an toàn
        players.AddMember(P("a", 100));
        Assert.AreEqual(0, t.IndexOfLowestHpAlly());   // 1 ally → index 0
    }

    [Test]
    public void SetEnemyChosenPlayer_DoesNotTouchAllyIndex()
    {
        var (t, players, _) = Setup();
        var a = P("a");
        players.AddMember(a);
        t.AllyIndex = 0;
        t.SetEnemyChosenPlayer(a);
        Assert.AreEqual(0, t.EnemyChosenPlayerIndex);
        Assert.AreEqual(0, t.AllyIndex);               // ally selection không bị nhiễu
    }
}
