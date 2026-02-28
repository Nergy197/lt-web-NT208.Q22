using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class EnemyAttackHit
{
    public bool canBeParried = true;

    // thời gian trước khi đòn đánh xảy ra
    public float windUpTime = 10f;

    // thời gian player có thể parry
    public float parryWindowDuration = 10f;

    public float damageMultiplier = 1f;

    public int repeat = 1;

    // ⭐ THÊM LẠI 2 FIELD NÀY để fix error
    public float delayBetweenHits = 0f;

    public List<float> timingOffsets = new List<float>();
}



public class EnemyAttack : AttackBase
{
    private List<EnemyAttackHit> hits;

    private EnemyStatus enemy;

    private PlayerStatus player;



    public EnemyAttack(string name, List<EnemyAttackHit> hits)
    {
        Name = name;

        this.hits = hits;
    }



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
        foreach (var hit in hits)
        {

            bool parried = false;



            // ================= OPEN PARRY WINDOW =================

            if (hit.canBeParried)
            {
                player.OpenParryWindow();

                Debug.Log("PARRY WINDOW OPEN for " + hit.parryWindowDuration + " seconds");

                float timer = 0;


                while (timer < hit.parryWindowDuration)
                {
                    if (player.ConsumeParry())
                    {
                        parried = true;

                        break;
                    }

                    timer += Time.deltaTime;

                    yield return null;
                }

                player.CloseParryWindow();

                Debug.Log("PARRY WINDOW CLOSED");
            }



            // ================= IMPACT =================

            if (!player.IsAlive)
                yield break;



            if (parried)
            {
                int counter = player.Atk / 2;

                enemy.TakeDamage(player, counter);

                Debug.Log("PARRY SUCCESS → COUNTER DAMAGE: " + counter);
            }
            else
            {
                int damage =
                    Mathf.RoundToInt(enemy.Atk * hit.damageMultiplier);

                player.TakeDamage(enemy, damage);

                Debug.Log("PLAYER HIT: " + damage);
            }

        }
    }



    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.5f);
    }
}