using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAttackHit
{
    public bool canBeParried = true;
    public float animDuration = 0.8f;
    public float parryOpenTime = 0.4f;
    public float damageMultiplier = 1f;
    public int apRestoreOnParry = 1;
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

    protected override bool DashesToTarget => hits != null && hits.Count > 0;

    public override void Use(Status attacker, Status target)
    {
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
        var effectiveHits = hits != null && hits.Count > 0
            ? hits
            : new List<EnemyAttackHit>
              { new EnemyAttackHit { animDuration = 0.8f, parryOpenTime = 0.4f,
                                     damageMultiplier = 1f, canBeParried = true,
                                     apRestoreOnParry = 1 } };

        bool anyHitParried = false;
        int hitIndex = 0;

        foreach (var hit in effectiveHits)
        {
            BattleEvents.RaiseEnemyHitIncoming(hit, hitIndex, enemy);
            hitIndex++;

            PlayAttackerAnimation(hit.animDuration);

            if (hit.canBeParried)
            {
                if (hit.parryOpenTime > 0f)
                    yield return new WaitForSeconds(hit.parryOpenTime);

                player.OpenParryWindow();
                Debug.Log($"PARRY WINDOW OPEN at {hit.parryOpenTime}s/{hit.animDuration}s");

                float parryDuration = hit.animDuration - hit.parryOpenTime;
                float timer = 0f;
                bool parried = false;
                while (timer < parryDuration)
                {
                    if (player.ConsumeParry()) { parried = true; break; }
                    timer += Time.deltaTime;
                    yield return null;
                }

                player.CloseParryWindow();

                if (!player.IsAlive) yield break;

                if (parried)
                {
                    anyHitParried = true;
                    player.RestoreAP(hit.apRestoreOnParry);
                    int counter = Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier * 1.5f);
                    enemy.TakeDamage(player, counter);
                    Debug.Log($"PARRY SUCCESS → counter {counter} dmg | AP +{hit.apRestoreOnParry}");
                }
                else
                {
                    int damage = Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier);
                    player.TakeDamage(enemy, damage);
                    Debug.Log($"PLAYER HIT: {damage}");
                }
            }
            else
            {
                if (hit.animDuration > 0f)
                    yield return new WaitForSeconds(hit.animDuration);

                if (!player.IsAlive) yield break;

                int damage = Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier);
                player.TakeDamage(enemy, damage);
                Debug.Log($"PLAYER HIT (no parry): {damage}");
            }
        }

        // --- SKILL EFFECTS ---
        // Effect nhắm vào player (SkillEffectTarget.Enemy) bị bỏ qua nếu player parry.
        if (effects != null && effects.Count > 0)
        {
            foreach (var entry in effects)
            {
                if (entry?.effect == null) continue;
                if (anyHitParried && entry.target == SkillEffectTarget.Enemy) continue;

                Status targetStatus = ResolveEffectTarget(entry.target);
                if (targetStatus == null) continue;

                targetStatus.ApplyStatusEffect(entry.effect);
                Debug.Log($"[ENEMY EFFECT] {entry.effect.effectName} -> {targetStatus.entityName} ({entry.target})");
            }
        }
    }

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Đối với enemy: Self = enemy, Enemy = player.</summary>
    Status ResolveEffectTarget(SkillEffectTarget targetType)
    {
        switch (targetType)
        {
            case SkillEffectTarget.Self:  return enemy;
            case SkillEffectTarget.Enemy: return player;
            default:                      return player;
        }
    }
}
