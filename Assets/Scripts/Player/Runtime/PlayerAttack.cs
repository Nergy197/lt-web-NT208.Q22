using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttackHit
{
    public float windUpTime;
    public float damageMultiplier;
    public int repeat = 1;
    public float delayBetweenHits = 0f;
    public List<float> timingOffsets = new List<float>();
}

public class PlayerAttack : AttackBase
{
    public int apCost;

    private List<PlayerAttackHit> hits;
    private List<SkillEffectEntry> effects;

    private PlayerStatus player;
    private EnemyStatus enemy;

    public PlayerAttack(
        string name,
        int apCost,
        List<PlayerAttackHit> hits,
        List<SkillEffectEntry> effects = null)
    {
        Name = name;
        this.apCost = apCost;
        this.hits = hits;
        this.effects = effects ?? new List<SkillEffectEntry>();
    }

    // Bắt buộc implement từ AttackBase
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

    // ================= PREPARE =================

    protected override IEnumerator Prepare()
    {
        player = attacker as PlayerStatus;
        enemy = target as EnemyStatus;

        if (player == null || enemy == null)
        {
            cancelled = true;
            yield break;
        }

        if (!player.CanUseAP(apCost))
        {
            cancelled = true;
            yield break;
        }

        player.UseAP(apCost);
        yield return null;
    }

    // ================= EXECUTE =================

    protected override IEnumerator Execute()
    {
        // --- DAMAGE HITS ---
        foreach (var hit in hits)
        {
            if (hit.windUpTime > 0f)
                yield return new WaitForSeconds(hit.windUpTime);

            if (!player.IsAlive || !enemy.IsAlive)
                yield break;

            for (int i = 0; i < Mathf.Max(1, hit.repeat); i++)
            {
                if (!player.IsAlive || !enemy.IsAlive)
                    yield break;

                int dmg = Mathf.RoundToInt(player.Atk * hit.damageMultiplier);
                enemy.TakeDamage(player, dmg);

                Debug.Log($"[ATTACK] {player.entityName} → {enemy.entityName} : {dmg} dmg");

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

                Debug.Log($"[EFFECT] {entry.effect.effectName} → {targetStatus.entityName} " +
                          $"({entry.target}) dur:{entry.effect.duration}");
            }
        }
    }

    // ================= RECOVERY =================

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.15f);
    }

    // ================= HELPERS =================

    /// <summary>
    /// Chọn target dựa theo SkillEffectTarget.
    /// LowestHPAlly: tìm ally còn sống có HP thấp nhất trong cùng Party với player.
    /// </summary>
    Status ResolveEffectTarget(SkillEffectTarget targetType)
    {
        switch (targetType)
        {
            case SkillEffectTarget.Self:
                return player;

            case SkillEffectTarget.Enemy:
                return enemy;

            case SkillEffectTarget.SelectedAlly:
                return GetSelectedAlly();

            default:
                return enemy;
        }
    }

    Status GetSelectedAlly()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.PlayerParty != null)
        {
            var ally = BattleManager.Instance.GetAllyTarget();
            if (ally != null) return ally;
        }

        return player;
    }
}
