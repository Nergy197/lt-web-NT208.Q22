using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyStatus : Status
{
    private readonly List<EnemyAttackData> attacks = new();
    private int baseExpReward = 20;
    private bool isAggressive = true;
    public GameObject battlePrefab; // Set bởi EnemyData.CreateStatus()

    public EnemyStatus(
        string name,
        int baseHP,
        int baseAtk,
        int baseDef,
        int baseSpd,
        int baseExpReward = 20,
        bool isAggressive = true
    ) : base(name, baseHP, baseAtk, baseDef, baseSpd)
    {
        this.baseExpReward = baseExpReward;
        this.isAggressive = isAggressive;
    }

    // ================= BATTLE LIFECYCLE =================

    /// <summary>
    /// BUG FIX: Enemy luôn reset HP về Max khi vào battle.
    /// Player KHÔNG override → giữ HP từ save data.
    /// </summary>
    public override void ResetForBattle(float baseDelay)
    {
        base.ResetForBattle(baseDelay);
        currentHP = MaxHP;
    }

    // ================= ATTACK =================
    public void AddAttack(EnemyAttackData attack)
    {
        if (attack == null) return;
        if (!attacks.Contains(attack))
            attacks.Add(attack);
    }

    public EnemyAttackData GetRandomAttack()
    {
        if (attacks.Count == 0)
            return null;

        return attacks[Random.Range(0, attacks.Count)];
    }

    // ================= EXP =================

    /// <summary>
    /// Tính EXP thưởng được cân bằng theo chênh lệch cấp.
    /// <br/>- Quái cao hơn Player → thưởng EXP nhiều hơn (bonus)
    /// <br/>- Quái quá yếu hơn Player → penalty (tối thiểu 10%)
    /// </summary>
    public int GetExpReward(int playerAvgLevel = 1)
    {
        float baseExp = baseExpReward * (1f + level * 0.2f);

        // Hệ số chênh lệch: mỗi cấp chênh = ±10%
        int diff = level - playerAvgLevel;
        float scale = 1f + diff * 0.1f;
        scale = Mathf.Clamp(scale, 0.1f, 2.0f);

        return Mathf.Max(1, Mathf.RoundToInt(baseExp * scale));
    }
}
