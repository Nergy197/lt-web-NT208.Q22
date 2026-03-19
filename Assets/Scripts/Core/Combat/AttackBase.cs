using System.Collections;
using UnityEngine;

public abstract class AttackBase
{
    public string Name { get; protected set; }
    public AttackPhase Phase { get; private set; }

    protected Status attacker;
    protected Status target;

    /// <summary>Đặt = true trong Prepare() nếu attack cần bị hủy (thiếu AP, target null, ...).</summary>
    protected bool cancelled = false;

    public void StartAttack(Status attacker, Status target)
    {
        this.attacker = attacker;
        this.target = target;
        cancelled = false;
        
        if (BattleRunner.Instance == null)
        {
            Debug.LogError("[ERROR] BattleRunner.Instance is null! Please add BattleRunner to scene.");
            BattleEvents.RaiseAttackFinished();
            return;
        }
        
        BattleRunner.Instance.StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Phase = AttackPhase.Prepare;
        yield return Prepare();

        // Nếu Prepare thất bại (cancelled hoặc attacker/target null) → skip Execute & Recovery
        if (!cancelled && attacker != null && target != null)
        {
            Phase = AttackPhase.Execute;
            yield return Execute();

            Phase = AttackPhase.Recovery;
            yield return Recovery();
        }

        Phase = AttackPhase.Finished;
        BattleEvents.RaiseAttackFinished();
    }

    protected virtual IEnumerator Prepare() { yield break; }
    protected abstract IEnumerator Execute();
    protected virtual IEnumerator Recovery() { yield break; }

    // Add abstract Use method for polymorphic attack execution
    public abstract void Use(Status attacker, Status target);
}
