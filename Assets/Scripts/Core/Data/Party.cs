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

    /// <summary>Số thành viên tối đa của party người chơi. Nguồn chân lý duy nhất — dùng chung với GameManager.</summary>
    public const int PlayerMaxMembers = 2;
    public const int EnemyMaxMembers = 3;

    public int MaxMembers => maxMembers;

    // ================= CONSTRUCTOR =================

    public Party(PartyType type)
    {
        Type = type;
        maxMembers = (type == PartyType.Player) ? PlayerMaxMembers : EnemyMaxMembers;
    }

    // ================= MEMBER =================

    public bool AddMember(Status status)
    {
        if (status == null)
        {
            UnityEngine.Debug.LogWarning($"[Party] AddMember bị từ chối: status null ({Type}).");
            return false;
        }

        if (members.Count >= maxMembers)
        {
            UnityEngine.Debug.LogWarning(
                $"[Party] AddMember bị từ chối: '{status.entityName}' — party {Type} đã đầy ({members.Count}/{maxMembers}).");
            return false;
        }

        if (members.Contains(status))
        {
            UnityEngine.Debug.LogWarning($"[Party] AddMember bị từ chối: '{status.entityName}' đã có trong party {Type}.");
            return false;
        }

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

    public void SwapMembers(int idx1, int idx2)
    {
        if (idx1 < 0 || idx1 >= members.Count || idx2 < 0 || idx2 >= members.Count)
            return;

        Status temp = members[idx1];
        members[idx1] = members[idx2];
        members[idx2] = temp;

        // Cập nhật lại BattleSlotId
        for (int i = 0; i < members.Count; i++)
            members[i].BattleSlotId = i;
    }

    public Status GetMemberBySlot(int slotId)
    {
        if (slotId < 0 || slotId >= members.Count)
            return null;

        return members[slotId];
    }

    public IEnumerable<Status> AliveMembers =>
        members.Where(m => m.IsAlive);
}
