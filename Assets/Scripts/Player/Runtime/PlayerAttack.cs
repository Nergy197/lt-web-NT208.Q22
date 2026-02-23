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
    public override void Use(Status attacker, Status target)
    {
        this.attacker = attacker;
        this.target = target;
        StartAttack(attacker, target); // Use public method from AttackBase
    }
    public int apCost;

    private List<PlayerAttackHit> hits;
    private PlayerStatus player;
    private EnemyStatus enemy;

    public PlayerAttack(string name, int apCost, List<PlayerAttackHit> hits)
    {
        Name = name;
        this.apCost = apCost;
        this.hits = hits;
    }

    protected override IEnumerator Prepare()
    {
        player = attacker as PlayerStatus;
        enemy = target as EnemyStatus;

        if (player == null || enemy == null)
            yield break;

        if (!player.CanUseAP(apCost))
            yield break;

        player.UseAP(apCost);
        yield return null;
    }

    protected override IEnumerator Execute()
    {
        foreach (var hit in hits)
        {
            // Wind-up once before the sequence of repeats
            if (hit.windUpTime > 0f)
                yield return new WaitForSeconds(hit.windUpTime);

            if (!player.IsAlive || !enemy.IsAlive)
                yield break;

            for (int i = 0; i < Mathf.Max(1, hit.repeat); i++)
            {
                if (!player.IsAlive || !enemy.IsAlive)
                    yield break;

                enemy.TakeDamage(player, Mathf.RoundToInt(player.Atk * hit.damageMultiplier));

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
        yield return new WaitForSeconds(0.15f);
    }
}
