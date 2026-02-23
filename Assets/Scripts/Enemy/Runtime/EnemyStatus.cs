using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyStatus : Status
{
    private readonly List<EnemyAttackData> attacks = new();
    private int baseExpReward = 20;
    private bool isAggressive = true;

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
    public int GetExpReward()
    {
        return Mathf.RoundToInt(baseExpReward * (1f + level * 0.2f));
    }
}
