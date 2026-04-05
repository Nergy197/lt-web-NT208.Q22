using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAttackHit
{
    public bool canBeParried = true;

    // Thoi gian enemy chuan bi truoc khi don danh xay ra (giay).
    // Player dung khoang nay de nhan biet animation va chuan bi parry.
    public float windUpTime = 0.5f;

    // Thoi gian player duoc phep bam parry sau khi wind-up ket thuc.
    public float parryWindowDuration = 1.5f;

    public float damageMultiplier = 1f;

    public int repeat = 1;

    public float delayBetweenHits = 0f;

    public List<float> timingOffsets = new List<float>();
}

public class EnemyAttack : AttackBase
{
    private List<EnemyAttackHit> hits;
    private List<SkillEffectEntry> effects;

    private EnemyStatus enemy;
    private PlayerStatus player;

    public EnemyAttack(string name, List<EnemyAttackHit> hits, List<SkillEffectEntry> effects = null)
    {
        Name = name;
        this.hits = hits;
        this.effects = effects ?? new List<SkillEffectEntry>();
    }

    public override void Use(Status attacker, Status target)
    {
        if (attacker.SpawnedModel != null)
        {
            var visual = attacker.SpawnedModel.GetComponent<UnitVisual>();
            if (visual != null) visual.PlayAttack();
        }

        this.attacker = attacker;
        this.target = target;
        StartAttack(attacker, target);
    }

    protected override IEnumerator Prepare()
    {
        enemy = attacker as EnemyStatus;
        player = target as PlayerStatus;
        yield return null;
    }

    protected override IEnumerator Execute()
    {
        // --- DAMAGE HITS ---
        foreach (var hit in hits)
        {
            if (hit.windUpTime > 0f)
                yield return new WaitForSeconds(hit.windUpTime);

            bool parried = false;

            if (hit.canBeParried)
            {
                player.OpenParryWindow();
                Debug.Log("PARRY WINDOW OPEN for " + hit.parryWindowDuration + " seconds");

                float timer = 0;
                while (timer < hit.parryWindowDuration)
                {
                    if (player.ConsumeParry()) { parried = true; break; }
                    timer += Time.deltaTime;
                    yield return null;
                }

                player.CloseParryWindow();
                Debug.Log("PARRY WINDOW CLOSED");
            }

            for (int i = 0; i < Mathf.Max(1, hit.repeat); i++)
            {
                if (!player.IsAlive) yield break;

                if (parried && i == 0)
                {
                    // Parry thanh cong o don dau -> player phan cong bang nua Atk.
                    int counter = player.Atk / 2;
                    enemy.TakeDamage(player, counter);
                    Debug.Log("PARRY SUCCESS -> COUNTER DAMAGE: " + counter);
                }
                else
                {
                    // Don binh thuong HOAC cac don repeat sau parry van gay damage.
                    int damage = Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier);
                    player.TakeDamage(enemy, damage);
                    Debug.Log($"PLAYER HIT [{i + 1}/{hit.repeat}]: " + damage);
                }

                if (i < hit.repeat - 1)
                {
                    float delay = hit.delayBetweenHits;
                    if (hit.timingOffsets != null && i < hit.timingOffsets.Count)
                        delay = hit.timingOffsets[i];
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }
            }
        }

        // --- SKILL EFFECTS (Buff / Debuff) ---
        if (effects != null && effects.Count > 0)
        {
            foreach (var entry in effects)
            {
                if (entry?.effect == null) continue;

                Status targetStatus = ResolveEffectTarget(entry.target);
                if (targetStatus == null) continue;

                targetStatus.ApplyStatusEffect(entry.effect);
                Debug.Log($"[ENEMY EFFECT] {entry.effect.effectName} -> {targetStatus.entityName} " +
                          $"({entry.target}) dur:{entry.effect.duration}");
            }
        }
    }

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.5f);
    }

    // ================= HELPERS =================

    /// <summary>
    /// Doi voi enemy: Self = enemy, Enemy = player (nguoc lai voi PlayerAttack).
    /// </summary>
    Status ResolveEffectTarget(SkillEffectTarget targetType)
    {
        switch (targetType)
        {
            case SkillEffectTarget.Self:
                return enemy;
            case SkillEffectTarget.Enemy:
                return player;
            default:
                return player;
        }
    }
}
