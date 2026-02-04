using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackHit
{
    public bool canBeParried;
    public float windUpTime;
    public float damageMultiplier;
    public int repeat = 1;
    public float delayBetweenHits = 0f;
    public List<float> timingOffsets = new List<float>();
}
public class EnemyAttack : AttackBase
{
    public override void Use(Status attacker, Status target)
    {
        this.attacker = attacker;
        this.target = target;
        StartAttack(attacker, target); // Use public method from AttackBase
    }
    private List<EnemyAttackHit> hits;
    private EnemyStatus enemy;
    private PlayerStatus player;

    public EnemyAttack(string name, List<EnemyAttackHit> hits)
    {
        Name = name;
        this.hits = hits;
    }

    protected override IEnumerator Prepare()
    {
        enemy = attacker as EnemyStatus;
        player = target as PlayerStatus;
        yield return null;
    }

    protected override IEnumerator Execute()
    {
        foreach (var hit in hits)
        {
            // Enable parry for the wind-up phase if applicable
            if (hit.canBeParried)
                player.EnableParry();

            if (hit.windUpTime > 0f)
                yield return new WaitForSeconds(hit.windUpTime);

            if (!player.IsAlive || !enemy.IsAlive)
                yield break;

            // Handle repeats: parry only considered for the first strike in the sequence
            bool first = true;
            for (int i = 0; i < Mathf.Max(1, hit.repeat); i++)
            {
                if (!player.IsAlive || !enemy.IsAlive)
                    yield break;

                if (hit.canBeParried && first && player.ConsumeParry())
                {
                    enemy.TakeDamage(player, player.Atk / 2);
                }
                else
                {
                    player.TakeDamage(enemy, Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier));
                }

                first = false;

                // Determine delay before next repeat: use timingOffsets[i] if available, else use delayBetweenHits
                if (i < hit.repeat - 1)
                {
                    float delay = hit.delayBetweenHits; // default
                    if (hit.timingOffsets != null && i < hit.timingOffsets.Count)
                        delay = hit.timingOffsets[i];
                    
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }
            }
        }
    }

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.2f);
    }
}
