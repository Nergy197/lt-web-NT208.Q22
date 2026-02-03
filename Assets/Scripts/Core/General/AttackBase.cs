using System.Collections;
using UnityEngine;

public abstract class AttackBase
{
    public string Name { get; protected set; }
    public AttackPhase Phase { get; private set; }

    protected Status attacker;
    protected Status target;

    public void StartAttack(Status attacker, Status target)
    {
        this.attacker = attacker;
        this.target = target;
        BattleRunner.Instance.StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        Phase = AttackPhase.Prepare;
        yield return Prepare();

        Phase = AttackPhase.Execute;
        yield return Execute();

        Phase = AttackPhase.Recovery;
        yield return Recovery();

        Phase = AttackPhase.Finished;
        BattleEvents.RaiseAttackFinished();
    }

    protected virtual IEnumerator Prepare() { yield break; }
    protected abstract IEnumerator Execute();
    protected virtual IEnumerator Recovery() { yield break; }
}
