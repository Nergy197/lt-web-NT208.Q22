using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerAttackHitData
{
    public float windUpTime = 0f;
    public float damageMultiplier = 1f;
    public int repeat = 1;
    public float delayBetweenHits = 0f; // default delay if timingOffsets is empty
    [Tooltip("Custom delay before each repeat (overrides delayBetweenHits). Leave empty to use delayBetweenHits for all. Example: [0, 0.2, 0.15] for 3 repeats.")]
    public List<float> timingOffsets = new List<float>(); // custom timing for each repeat
}

[CreateAssetMenu(fileName = "PlayerAttackData", menuName = "Game/PlayerAttack")]
public class PlayerAttackData : ScriptableObject
{
    public string attackName = "New Player Attack";
    public int apCost = 2;
    public List<PlayerAttackHitData> hits = new List<PlayerAttackHitData>();

    public PlayerAttack CreateInstance()
    {
        var hitObjs = new List<PlayerAttackHit>();
        foreach (var h in hits)
        {
            var hit = new PlayerAttackHit 
            { 
                windUpTime = h.windUpTime, 
                damageMultiplier = h.damageMultiplier, 
                repeat = Mathf.Max(1, h.repeat), 
                delayBetweenHits = h.delayBetweenHits,
                timingOffsets = new List<float>(h.timingOffsets) // copy custom timings
            };
            hitObjs.Add(hit);
        }
        return new PlayerAttack(attackName, apCost, hitObjs);
    }
}
