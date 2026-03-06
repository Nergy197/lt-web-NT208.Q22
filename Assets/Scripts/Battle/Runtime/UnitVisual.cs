using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnitVisual : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayAttack()
    {
        if (animator != null) animator.SetTrigger("doAttack");
    }

    public void PlayParry()
    {
        if (animator != null) animator.SetTrigger("doParry");
    }

    public void PlayHit()
    {
        if (animator != null) animator.SetTrigger("takeHit");
    }

    public void PlayDie()
    {
        if (animator != null) animator.SetTrigger("die");
    }
}
