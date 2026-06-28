using NUnit.Framework;

public class PartyTests
{
    static PlayerStatus NewPlayer(string name) => new PlayerStatus(name, 100, 20, 10, 15);
    static EnemyStatus NewEnemy(string name) => new EnemyStatus(name, 50, 15, 5, 4);

    [Test]
    public void PlayerParty_CapsAtTwoMembers()
    {
        var party = new Party(PartyType.Player);
        Assert.IsTrue(party.AddMember(NewPlayer("A")));
        Assert.IsTrue(party.AddMember(NewPlayer("B")));
        // Thành viên thứ 3 phải bị từ chối (cap = 2).
        Assert.IsFalse(party.AddMember(NewPlayer("C")));
        Assert.AreEqual(2, party.Members.Count);
        Assert.AreEqual(Party.PlayerMaxMembers, party.Members.Count);
    }

    [Test]
    public void EnemyParty_CapsAtThreeMembers()
    {
        var party = new Party(PartyType.Enemy);
        Assert.IsTrue(party.AddMember(NewEnemy("A")));
        Assert.IsTrue(party.AddMember(NewEnemy("B")));
        Assert.IsTrue(party.AddMember(NewEnemy("C")));
        Assert.IsFalse(party.AddMember(NewEnemy("D")));
        Assert.AreEqual(3, party.Members.Count);
    }

    [Test]
    public void AddMember_RejectsNullAndDuplicate()
    {
        var party = new Party(PartyType.Player);
        Assert.IsFalse(party.AddMember(null));

        var p = NewPlayer("A");
        Assert.IsTrue(party.AddMember(p));
        Assert.IsFalse(party.AddMember(p)); // trùng tham chiếu
        Assert.AreEqual(1, party.Members.Count);
    }

    [Test]
    public void AddMember_AssignsBattleSlotAndPartyType()
    {
        var party = new Party(PartyType.Player);
        var a = NewPlayer("A");
        var b = NewPlayer("B");
        party.AddMember(a);
        party.AddMember(b);

        Assert.AreEqual(0, a.BattleSlotId);
        Assert.AreEqual(1, b.BattleSlotId);
        Assert.AreEqual(PartyType.Player, a.PartyType);
    }
}
