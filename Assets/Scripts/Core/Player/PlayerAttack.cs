using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerAttackHit
{
    public float windUpTime;
    public float damageMultiplier;
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
            if (hit.windUpTime > 0)
                yield return new WaitForSeconds(hit.windUpTime);

            if (!player.IsAlive || !enemy.IsAlive)
                yield break;

            enemy.TakeDamage(player, Mathf.RoundToInt(player.Atk * hit.damageMultiplier));
        }
    }

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.15f);
    }
}
