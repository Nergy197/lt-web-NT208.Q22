using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class EnemyStatus : Status
{
    [SerializeField] private List<EnemyAttack> attacks = new List<EnemyAttack>();
    [SerializeField] private int baseExpReward = 20;

    [SerializeField] private bool isAggressive = true; 

    public EnemyStatus(string name, int baseHP, int baseAtk, int baseDef, int baseSpd)
        : base(name, baseHP, baseAtk, baseDef, baseSpd){}
    
    public int GetExpReward()
    {
        return Mathf.RoundToInt(baseExpReward * (1f + level * 0.2f));
    }

    public EnemyAttack GetAttack()
    {
        if (attacks == null || attacks.Count == 0)
            return null;

        // Core đơn giản: random
        return attacks[Random.Range(0, attacks.Count)];
    }

    // Allow runtime assignment of attack instances created from Editor data
    public void SetAttackInstances(System.Collections.Generic.List<EnemyAttack> instances)
    {
        this.attacks = instances ?? new System.Collections.Generic.List<EnemyAttack>();
    }

}