using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UnitVisual : MonoBehaviour
{
    [Header("Dash Settings")]
    [Tooltip("Tốc độ dash về phía mục tiêu (units/giây).")]
    public float dashSpeed = 8f;
    [Tooltip("Khoảng cách dừng lại trước mục tiêu khi dash.")]
    public float dashStopDistance = 0.8f;
    [Tooltip("Tốc độ quay về vị trí ban đầu.")]
    public float returnSpeed = 10f;

    private Animator animator;
    private Vector3 originPosition;
    private bool originRecorded;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // ── Animator triggers ─────────────────────────────────────────────────────

    public void PlayAttack() { if (animator) animator.SetTrigger("doAttack"); }
    public void PlayParry()  { if (animator) animator.SetTrigger("doParry");  }
    public void PlayHit()    { if (animator) animator.SetTrigger("takeHit");  }
    public void PlayDie()    { if (animator) animator.SetTrigger("die");      }

    // ── Dash movement ─────────────────────────────────────────────────────────

    /// <summary>Dash về phía target, dừng cách target một khoảng dashStopDistance.</summary>
    public IEnumerator DashToward(Transform target)
    {
        if (target == null) yield break;

        if (!originRecorded)
        {
            originPosition = transform.position;
            originRecorded = true;
        }

        // Tắt root motion để animation không tranh chấp với code movement
        if (animator) animator.applyRootMotion = false;

        while (Vector3.Distance(transform.position, target.position) > dashStopDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target.position, dashSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>Quay về vị trí ban đầu sau khi tấn công xong.</summary>
    public IEnumerator ReturnToOrigin()
    {
        while (Vector3.Distance(transform.position, originPosition) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, originPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = originPosition;

        // Bật lại root motion sau khi về chỗ
        if (animator) animator.applyRootMotion = true;
    }
}
