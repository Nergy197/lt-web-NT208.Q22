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
    [Header("Sprite Reference")]
    [Tooltip("Kéo Body SpriteRenderer vào đây. Nếu để trống sẽ tự tìm child tên 'Body'.")]
    public SpriteRenderer bodyRenderer;

    private Vector3 originPosition;
    private bool originRecorded;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (bodyRenderer == null)
            bodyRenderer = FindBodyRenderer();
    }

    SpriteRenderer FindBodyRenderer()
    {
        // Tìm chính xác child tên "Body" để tránh nhầm Weapon/Shield
        foreach (Transform child in transform)
        {
            if (child.name == "Body")
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) return sr;
            }
        }
        return GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>Tâm sprite trong world space (dùng cho cursor và dash targeting).</summary>
    public Vector3 SpriteCenter => bodyRenderer != null ? bodyRenderer.bounds.center : transform.position;

    /// <summary>+1 nếu sprite mặt về phải, -1 nếu mặt về trái (tự động dựa theo vị trí sân đấu).</summary>
    public float FacingSign
    {
        get
        {
            float baseFace = transform.position.x > 0f ? -1f : 1f;
            float scaleSign = transform.localScale.x >= 0f ? 1f : -1f;
            return baseFace * scaleSign;
        }
    }

    // ── Animator triggers ─────────────────────────────────────────────────────

    public void PlayAttack() { if (animator) animator.SetTrigger("doAttack"); }
    public void PlayParry()  { if (animator) animator.SetTrigger("doParry");  }
    public void PlayHit()    { if (animator) animator.SetTrigger("takeHit");  }
    public void PlayDie()    { if (animator) animator.SetTrigger("die");      }

    /// <summary>Đặt tốc độ phát toàn bộ animation. Dùng để căn clip với animDuration của hit.</summary>
    public void SetAnimatorSpeed(float speed) { if (animator) animator.speed = speed; }

    // ── Dash movement ─────────────────────────────────────────────────────────

    /// <summary>Dash về phía target, dừng khi cạnh sprite của attacker cách cạnh sprite của target một khoảng dashStopDistance.</summary>
    public IEnumerator DashToward(Transform target)
    {
        if (target == null) yield break;

        if (!originRecorded)
        {
            originPosition = transform.position;
            originRecorded = true;
        }

        if (animator) animator.applyRootMotion = false;

        // Yield 1 frame để SpriteRenderer.bounds được Unity tính sau khi spawn
        yield return null;

        var targetVisual = target.GetComponent<UnitVisual>();
        Vector3 targetCenter = targetVisual != null ? targetVisual.SpriteCenter : target.position;

        float myHalfX     = bodyRenderer  != null ? bodyRenderer.bounds.extents.x  : 0f;
        float targetHalfX = targetVisual  != null && targetVisual.bodyRenderer != null
                            ? targetVisual.bodyRenderer.bounds.extents.x : 0f;
        float stopDist    = myHalfX + targetHalfX + dashStopDistance;

        // Offset giữa transform pivot và sprite center — cố định suốt coroutine
        // Dùng để tính đúng vị trí transform cần đến, tránh drift theo trục Y
        Vector3 spriteOffset  = SpriteCenter - transform.position;
        Vector3 rawDir        = targetCenter - SpriteCenter;
        Vector3 direction     = rawDir.sqrMagnitude > 0.0001f ? rawDir.normalized : Vector3.right;

        // Đã trong tầm tấn công — không cần dash
        if (Vector3.Distance(SpriteCenter, targetCenter) <= stopDist)
            yield break;

        Vector3 stopSpritePos = targetCenter - direction * stopDist;
        Vector3 moveTarget    = stopSpritePos - spriteOffset;

        float dashTimeout = 4f;
        float elapsed = 0f;
        while (true)
        {
            if (Vector3.Distance(SpriteCenter, targetCenter) <= stopDist)
                break;

            elapsed += Time.deltaTime;
            if (elapsed >= dashTimeout)
            {
                Debug.LogWarning($"[VISUAL] DashToward timeout ({dashTimeout}s) — snap to stop position");
                transform.position = moveTarget;
                break;
            }

            transform.position = Vector3.MoveTowards(
                transform.position, moveTarget, dashSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>Quay về vị trí ban đầu sau khi tấn công xong.</summary>
    public IEnumerator ReturnToOrigin()
    {
        if (!originRecorded)
        {
            // originPosition chưa được ghi — snap về vị trí hiện tại, không cần di chuyển
            yield break;
        }

        float returnTimeout = 4f;
        float elapsed = 0f;
        while (Vector3.Distance(transform.position, originPosition) > 0.02f)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= returnTimeout)
            {
                Debug.LogWarning($"[VISUAL] ReturnToOrigin timeout ({returnTimeout}s) — snap");
                break;
            }
            transform.position = Vector3.MoveTowards(
                transform.position, originPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = originPosition;

        // Bật lại root motion sau khi về chỗ
        if (animator) animator.applyRootMotion = true;
    }
}
