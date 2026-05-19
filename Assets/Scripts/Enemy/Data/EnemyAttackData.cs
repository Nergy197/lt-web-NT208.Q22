using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAttackHitData
{
    public bool canBeParried = true;
    [Tooltip("Tổng thời gian animation của hit này (giây). Xác định tốc độ phát clip.")]
    public float animDuration = 0.8f;
    [Tooltip("Thời điểm trong animation parry window mở (giây). Window đóng khi animation kết thúc.")]
    public float parryOpenTime = 0.4f;
    public float damageMultiplier = 1f;
    [Tooltip("AP trả lại cho player khi parry thành công đòn này.")]
    public int apRestoreOnParry = 1;
}

[CreateAssetMenu(fileName = "EnemyAttackData", menuName = "Game/EnemyAttack")]
public class EnemyAttackData : ScriptableObject
{
    public string attackName = "New Enemy Attack";
    public List<EnemyAttackHitData> hits = new List<EnemyAttackHitData>();

    [Header("Skill Effects (Buff / Debuff)")]
    [Tooltip("Effects applied after all hits. Self = enemy, Enemy = player. Bị bỏ qua nếu player parry.")]
    public List<SkillEffectEntry> effects = new List<SkillEffectEntry>();

    public EnemyAttack CreateInstance()
    {
        var hitObjs = new List<EnemyAttackHit>();
        foreach (var h in hits)
        {
            hitObjs.Add(new EnemyAttackHit
            {
                canBeParried     = h.canBeParried,
                animDuration     = h.animDuration,
                parryOpenTime    = h.parryOpenTime,
                damageMultiplier = h.damageMultiplier,
                apRestoreOnParry = h.apRestoreOnParry,
            });
        }

        var effectObjs = new List<SkillEffectEntry>(effects);
        return new EnemyAttack(attackName, hitObjs, effectObjs);
    }
}
