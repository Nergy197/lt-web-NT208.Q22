using System.Collections;
using UnityEngine;

public abstract class AttackBase
{
    public string Name { get; protected set; }
    public AttackPhase Phase { get; private set; }

    protected Status attacker;
    protected Status target;

    protected bool cancelled = false;

    public void StartAttack(Status attacker, Status target)
    {
        this.attacker = attacker;
        this.target = target;
        cancelled = false;

        if (BattleRunner.Instance == null)
        {
            Debug.LogError("[ERROR] BattleRunner.Instance is null!");
            BattleEvents.RaiseAttackFinished();
            return;
        }

        BattleRunner.Instance.StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Phase = AttackPhase.Prepare;
        yield return Prepare();

        if (!cancelled && attacker != null && target != null)
        {
            var visual = attacker.SpawnedModel?.GetComponent<UnitVisual>();
            var targetTransform = target.SpawnedModel?.transform;

            // Chỉ dash khi chiêu có đòn tấn công trực tiếp
            if (DashesToTarget && visual != null && targetTransform != null)
                yield return visual.DashToward(targetTransform);

            Phase = AttackPhase.Execute;
            yield return Execute();

            // Reset tốc độ animation về bình thường sau khi execute xong
            visual?.SetAnimatorSpeed(1f);

            if (DashesToTarget && visual != null)
                yield return visual.ReturnToOrigin();

            Phase = AttackPhase.Recovery;
            yield return Recovery();
        }

        Phase = AttackPhase.Finished;
        BattleEvents.RaiseAttackFinished();
    }

    // Thời lượng tự nhiên của clip tấn công (giây). Chỉnh cho khớp với clip thực tế.
    private const float ReferenceAnimDuration = 0.5f;

    /// <summary>Gọi animation tấn công, tự điều chỉnh tốc độ để clip phát đúng animDuration giây.</summary>
    protected void PlayAttackerAnimation(float animDuration)
    {
        var visual = attacker?.SpawnedModel?.GetComponent<UnitVisual>();
        if (visual == null) return;
        float speed = animDuration > 0f
            ? Mathf.Clamp(ReferenceAnimDuration / animDuration, 0.1f, 4f)
            : 1f;
        visual.SetAnimatorSpeed(speed);
        visual.PlayAttack();
    }

    /// <summary>Gọi animation tấn công ở tốc độ bình thường (dùng khi không có windUpTime cụ thể).</summary>
    protected void PlayAttackerAnimation()
    {
        attacker?.SpawnedModel?.GetComponent<UnitVisual>()?.PlayAttack();
    }

    /// <summary>Override thành false để chiêu buff không dash về phía mục tiêu.</summary>
    protected virtual bool DashesToTarget => true;

    protected virtual IEnumerator Prepare() { yield break; }
    protected abstract IEnumerator Execute();
    protected virtual IEnumerator Recovery() { yield break; }

    public abstract void Use(Status attacker, Status target);
}
