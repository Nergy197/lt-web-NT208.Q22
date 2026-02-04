using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackHit
{
    public bool canBeParried;
    public float windUpTime;
    public float damageMultiplier;
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
            if (hit.canBeParried)
                player.EnableParry();

            yield return new WaitForSeconds(hit.windUpTime);

            if (hit.canBeParried && player.ConsumeParry())
                enemy.TakeDamage(player, player.Atk / 2);
            else
                player.TakeDamage(enemy, Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier));
        }
    }

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.2f);
    }
}
