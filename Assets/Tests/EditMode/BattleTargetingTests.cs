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
    public void IndexOfLowestHpAlly_PicksLowestRatio()
    {
        var (t, players, _) = Setup();
        var a = P("a", 100);
        var b = P("b", 100);
        players.AddMember(a);
        players.AddMember(b);
        b.TakeDamage(null, 60);                  // b còn ~40% HP
        Assert.AreEqual(1, t.IndexOfLowestHpAlly());
    }

    [Test]
    public void SetEnemyChosenPlayer_RecordsIndexWithoutTouchingAllyIndex()
    {
        var (t, players, _) = Setup();
        var a = P("a");
        var b = P("b");
        players.AddMember(a);
        players.AddMember(b);
        t.AllyIndex = 0;
        t.SetEnemyChosenPlayer(b);
        Assert.AreEqual(1, t.EnemyChosenPlayerIndex);
        Assert.AreEqual(0, t.AllyIndex);         // ally selection không bị nhiễu
    }
}
