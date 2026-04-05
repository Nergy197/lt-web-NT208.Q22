using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyStatus : Status
{
    private readonly List<EnemyAttackData> attacks = new();
    private int baseExpReward = 20;
    private bool isAggressive = true;
    public GameObject battlePrefab;
    public EnemyAI ai;

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

    /// <summary>
    /// Enemy luon reset HP ve Max khi vao battle vi chung khong duoc luu giua cac tran.
    /// Player khong override -> giu HP tu save data.
    /// </summary>
    public override void ResetForBattle(float baseDelay)
    {
        base.ResetForBattle(baseDelay);
        currentHP = MaxHP;
    }

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

    /// <summary>
    /// Tra ve toan bo danh sach attack de AI co the danh gia va lua chon.
    /// </summary>
    public IReadOnlyList<EnemyAttackData> GetAllAttacks() => attacks;

    /// <summary>
    /// Kiem tra enemy co AI va co the goi CalculateAction().
    /// </summary>
    public bool HasAI => ai != null;

    /// <summary>
    /// EXP thuong duoc can bang theo chenh lech cap giua quai va player:
    /// quai cao cap hon -> EXP thuong nhieu hon (toi da x2), quai qua yeu -> toi thieu 10%.
    /// </summary>
    public int GetExpReward(int playerAvgLevel = 1)
    {
        float baseExp = baseExpReward * (1f + level * 0.2f);

        int diff = level - playerAvgLevel;
        float scale = Mathf.Clamp(1f + diff * 0.1f, 0.1f, 2.0f);

        return Mathf.Max(1, Mathf.RoundToInt(baseExp * scale));
    }
}

