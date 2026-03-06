using System.Collections.Generic;
using UnityEngine;

// ===================== SKILL EFFECT =====================

public enum SkillEffectTarget
{
    Self,           // áp lên người dùng skill
    Enemy,          // áp lên enemy bị nhắm
    SelectedAlly    // áp lên đồng đội được chỉ định (tùy chọn)
}

[System.Serializable]
public class SkillEffectEntry
{
    [Tooltip("Chọn StatusEffect để áp dụng")]
    public StatusEffect effect;

    [Tooltip("Áp lên ai?")]
    public SkillEffectTarget target = SkillEffectTarget.Enemy;
}

// ===================== HIT DATA =====================

[System.Serializable]
public class PlayerAttackHitData
{
    public float windUpTime = 0f;
    public float damageMultiplier = 1f;
    public int repeat = 1;
    public float delayBetweenHits = 0f;
    [Tooltip("Custom delay before each repeat. Leave empty to use delayBetweenHits.")]
    public List<float> timingOffsets = new List<float>();
}

// ===================== ATTACK DATA (ScriptableObject) =====================

[CreateAssetMenu(fileName = "PlayerAttackData", menuName = "Game/PlayerAttack")]
public class PlayerAttackData : ScriptableObject
{
    [Header("Info")]
    public string attackName = "New Player Attack";
    public int apCost = 2;

    [Header("Damage Hits")]
    [Tooltip("Để trống nếu skill chỉ là buff/debuff (không gây damage)")]
    public List<PlayerAttackHitData> hits = new List<PlayerAttackHitData>();

    [Header("Skill Effects (Buff / Debuff)")]
    [Tooltip("Để trống nếu skill chỉ là đòn đánh thuần túy")]
    public List<SkillEffectEntry> effects = new List<SkillEffectEntry>();

    public PlayerAttack CreateInstance()
    {
        var hitObjs = new List<PlayerAttackHit>();
        foreach (var h in hits)
        {
            hitObjs.Add(new PlayerAttackHit
            {
                windUpTime = h.windUpTime,
                damageMultiplier = h.damageMultiplier,
                repeat = Mathf.Max(1, h.repeat),
                delayBetweenHits = h.delayBetweenHits,
                timingOffsets = new List<float>(h.timingOffsets)
            });
        }

        // Clone danh sách effects
        var effectObjs = new List<SkillEffectEntry>(effects);

        return new PlayerAttack(attackName, apCost, hitObjs, effectObjs);
    }
}
