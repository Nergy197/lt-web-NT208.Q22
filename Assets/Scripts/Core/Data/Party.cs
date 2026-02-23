using System.Collections.Generic;
using System.Linq;

public enum PartyType
{
    Player,
    Enemy
}

public class Party
{
    // ================= CONFIG =================

    public PartyType Type { get; private set; }

    private List<Status> members = new List<Status>();
    public IReadOnlyList<Status> Members => members;

    private int maxMembers;

    // ================= CONSTRUCTOR =================

    public Party(PartyType type)
    {
        Type = type;
        maxMembers = (type == PartyType.Player) ? 2 : 3;
    }

    // ================= MEMBER =================

    public bool AddMember(Status status)
    {
        if (status == null)
            return false;

        if (members.Count >= maxMembers)
            return false;

        if (members.Contains(status))
            return false;

        // gán party type cho status (RẤT QUAN TRỌNG)
        status.PartyType = Type;

        // slot ID = vị trí trong party
        status.BattleSlotId = members.Count;

        members.Add(status);
        return true;
    }

    // ================= STATE =================

    public bool IsDefeated()
    {
        return members.All(m => !m.IsAlive);
    }

    // ================= SLOT HELPERS =================

    public Status GetMemberBySlot(int slotId)
    {
        if (slotId < 0 || slotId >= members.Count)
            return null;

        return members[slotId];
    }

    public IEnumerable<Status> AliveMembers =>
        members.Where(m => m.IsAlive);
}
