using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAttackHitData
{
    public bool canBeParried = true;
    public float windUpTime = 0.5f;
    public float damageMultiplier = 1f;
    public int repeat = 1;
    public float delayBetweenHits = 0f; // default delay if timingOffsets is empty
    [Tooltip("Custom delay before each repeat (overrides delayBetweenHits). Leave empty to use delayBetweenHits for all. Example: [0, 0.2, 0.15] for 3 repeats.")]
    public List<float> timingOffsets = new List<float>(); // custom timing for each repeat
}

[CreateAssetMenu(fileName = "EnemyAttackData", menuName = "Game/EnemyAttack")]
public class EnemyAttackData : ScriptableObject
{
    public string attackName = "New Enemy Attack";
    public List<EnemyAttackHitData> hits = new List<EnemyAttackHitData>();

    public EnemyAttack CreateInstance()
    {
        var hitObjs = new List<EnemyAttackHit>();
        foreach (var h in hits)
        {
            var hit = new EnemyAttackHit 
            { 
                canBeParried = h.canBeParried, 
                windUpTime = h.windUpTime, 
                damageMultiplier = h.damageMultiplier, 
                repeat = Mathf.Max(1, h.repeat), 
                delayBetweenHits = h.delayBetweenHits,
                timingOffsets = new List<float>(h.timingOffsets)
            };
            hitObjs.Add(hit);
        }
        return new EnemyAttack(attackName, hitObjs);
    }
}
