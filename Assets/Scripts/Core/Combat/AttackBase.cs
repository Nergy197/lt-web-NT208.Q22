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
            // Lướt tới mục tiêu trước khi ra đòn
            var visual = attacker.SpawnedModel?.GetComponent<UnitVisual>();
            var targetTransform = target.SpawnedModel?.transform;
            if (visual != null && targetTransform != null)
                yield return visual.DashToward(targetTransform);

            // Execute tự gọi PlayAttack() đúng thời điểm cho từng hit
            Phase = AttackPhase.Execute;
            yield return Execute();

            // Lướt về sau khi đánh xong
            if (visual != null)
                yield return visual.ReturnToOrigin();

            Phase = AttackPhase.Recovery;
            yield return Recovery();
        }

        Phase = AttackPhase.Finished;
        BattleEvents.RaiseAttackFinished();
    }

    /// <summary>Gọi animation tấn công trên model của attacker.</summary>
    protected void PlayAttackerAnimation()
    {
        attacker?.SpawnedModel?.GetComponent<UnitVisual>()?.PlayAttack();
    }

    protected virtual IEnumerator Prepare() { yield break; }
    protected abstract IEnumerator Execute();
    protected virtual IEnumerator Recovery() { yield break; }

    public abstract void Use(Status attacker, Status target);
}
