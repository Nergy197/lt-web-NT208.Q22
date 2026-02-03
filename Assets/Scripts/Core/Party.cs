using System.Collections.Generic;
using System.Linq;

public enum PartyType
{
    Player,
    Enemy
}

public class Party
{
    private List<Character> members = new List<Character>();
    public IReadOnlyList<Character> Members => members;

    private int maxMembers;

    public Party(PartyType type)
    {
        maxMembers = (type == PartyType.Player) ? 2 : 3;
    }

    public bool AddMember(Character character)
    {
        if (members.Count >= maxMembers)
            return false;

        if (members.Contains(character))
            return false;

        members.Add(character);
        return true;
    }

    public bool IsDefeated()
    {
        return members.All(c => !c.IsAlive);
    }
}
