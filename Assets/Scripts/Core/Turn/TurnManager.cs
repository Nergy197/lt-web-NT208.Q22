using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý timeline speed-based cho hệ thống turn-based.
/// Unit có Spd cao hơn sẽ hành động thường xuyên hơn.
/// BattleManager tạo instance → gọi AddUnit → lặp GetNextUnit mỗi vòng.
/// </summary>
public class TurnManager
{
    private readonly List<Status> timeline = new();
    private readonly float baseDelay;

    /// <summary>Số unit còn sống trong timeline.</summary>
    public int AliveCount => timeline.Count;

    /// <summary>Unit đang hành động (được set bởi GetNextUnit).</summary>
    public Status CurrentUnit { get; private set; }

    // ================= CONSTRUCTOR =================

    public TurnManager(float baseDelay = 100f)
    {
        this.baseDelay = baseDelay;
    }

    // ================= SETUP =================

    /// <summary>
    /// Thêm toàn bộ thành viên của party vào timeline.
    /// Tự động gọi ResetForBattle để clear effects cũ và tính NextTurnTime ban đầu.
    /// </summary>
    public void AddParty(Party party)
    {
        foreach (var s in party.Members)
        {
            s.ResetForBattle(baseDelay);
            timeline.Add(s);
            Debug.Log("[TURN] Added: " + s.entityName);
        }
    }

    /// <summary>Xóa toàn bộ timeline (gọi khi bắt đầu trận mới).</summary>
    public void Clear()
    {
        timeline.Clear();
        CurrentUnit = null;
    }

    // ================= TURN LOGIC =================

    /// <summary>
    /// Lấy unit tiếp theo trong timeline.
    /// Tự động xóa unit chết, sắp xếp theo NextTurnTime,
    /// và cập nhật NextTurnTime cho unit được chọn.
    /// </summary>
    public Status GetNextUnit()
    {
        // Xóa unit đã chết khỏi timeline
        timeline.RemoveAll(u => !u.IsAlive);

        if (timeline.Count == 0)
        {
            CurrentUnit = null;
            return null;
        }

        // Sắp xếp theo NextTurnTime tăng dần → unit "đến lượt sớm nhất" đứng đầu
        timeline.Sort((a, b) => a.NextTurnTime.CompareTo(b.NextTurnTime));

        var unit = timeline[0];
        unit.NextTurnTime += baseDelay / Mathf.Max(1, unit.Spd);

        CurrentUnit = unit;
        return unit;
    }

    // ================= TURN EFFECTS =================

    /// <summary>
    /// Xử lý Poison damage và Stun ở đầu mỗi lượt.
    /// Trả về true nếu unit bị Stun (mất lượt hành động).
    /// </summary>
    public bool ProcessTurnEffects(Status unit)
    {
        bool stunned = false;
        var effects = unit.GetActiveEffects();

        foreach (var effect in effects)
        {
            switch (effect.effectType)
            {
                case StatusEffectType.Poison:
                    if (unit.IsAlive && effect.value > 0)
                    {
                        unit.TakePoisonDamage(effect.value);
                        Debug.Log($"[POISON] {unit.entityName} mất {effect.value} HP (còn {unit.currentHP})");
                    }
                    break;

                case StatusEffectType.Stun:
                    stunned = true;
                    break;
            }
        }

        return stunned;
    }
}
