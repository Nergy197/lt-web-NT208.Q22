using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAttackHit
{
    public bool canBeParried = true;

    // Thời gian enemy chuẩn bị trước khi đòn đánh xảy ra (giây).
    // Player dùng khoảng này để nhận biết animation và chuẩn bị parry.
    public float windUpTime = 0.5f;

    // Thời gian player được phép bấm parry sau khi wind-up kết thúc.
    public float parryWindowDuration = 1.5f;

    public float damageMultiplier = 1f;

    public int repeat = 1;

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
        foreach (var hit in hits)
        {
            // Wait cho animation wind-up của enemy trước khi mở cửa sổ parry,
            // để player có thể nhìn thấy và phản응 đúng lúc.
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
                    // Parry thành công ở đòn đầu → player phản công bằng nửa Atk.
                    int counter = player.Atk / 2;
                    enemy.TakeDamage(player, counter);
                    Debug.Log("PARRY SUCCESS → COUNTER DAMAGE: " + counter);
                }
                else if (!parried)
                {
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
    }

    protected override IEnumerator Recovery()
    {
        yield return new WaitForSeconds(0.5f);
    }
}
