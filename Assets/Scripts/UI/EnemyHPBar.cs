using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HP bar của enemy — co dãn chiều rộng theo HP thay vì dùng fillAmount.
/// Không cần sprite, không cần Image.Type.Filled.
/// </summary>
public class EnemyHPBar : MonoBehaviour
{
    public Status trackedEnemy;
    public RectTransform fillRect; // rect của fill (không phải Image.fill)
    public Vector3 worldOffset;

    private Camera cam;
    private RectTransform rt;

    void Start()
    {
        cam = Camera.main;
        rt  = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (trackedEnemy == null || fillRect == null) return;

        if (PauseMenuUI.IsPaused)
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            return;
        }

        // Co dãn chiều rộng fill theo tỉ lệ HP
        float ratio = trackedEnemy.MaxHP > 0
            ? Mathf.Clamp01((float)trackedEnemy.currentHP / trackedEnemy.MaxHP)
            : 0f;

        fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);

        // Theo vị trí world của enemy
        if (cam != null && trackedEnemy.SpawnedModel != null)
        {
            var t = trackedEnemy.SpawnedModel.transform;
            var visual = trackedEnemy.SpawnedModel.GetComponent<UnitVisual>();

            // X: tâm sprite nếu có, không thì dùng transform
            float wx = (visual != null && visual.bodyRenderer != null)
                ? visual.SpriteCenter.x
                : t.position.x;

            // Y: transform.position + worldOffset (đáng tin hơn bounds.min.y
            //    vì sprite anchor của BinhLinh có thể nằm ở bất kỳ đâu)
            float wy = t.position.y + worldOffset.y;

            Vector3 world = new Vector3(wx, wy, t.position.z);

            // Dùng viewport coordinates để hoạt động đúng với mọi Canvas render mode
            Vector3 vp = cam.WorldToViewportPoint(world);
            rt.anchorMin = new Vector2(vp.x, vp.y);
            rt.anchorMax = new Vector2(vp.x, vp.y);
            rt.anchoredPosition = Vector2.zero;
        }

        gameObject.SetActive(trackedEnemy.IsAlive);
    }
}
