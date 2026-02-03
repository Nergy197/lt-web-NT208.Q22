using System.Collections.Generic;
using System.Linq;

public enum PartyType
{
    Player,
    Enemy
}

public class Party
{
    private List<Status> members = new List<Status>();
    public IReadOnlyList<Status> Members => members;

    private int maxMembers;

    public Party(PartyType type)
    {
        maxMembers = (type == PartyType.Player) ? 2 : 3;
    }

    public bool AddMember(Status status)
    {
        if (members.Count >= maxMembers)
            return false;

        if (members.Contains(status))
            return false;

        members.Add(status);
        return true;
    }

    public bool IsDefeated()
    {
        return members.All(m => !m.IsAlive);
    }
}
