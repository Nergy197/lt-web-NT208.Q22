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

        // Co dãn chiều rộng fill theo tỉ lệ HP
        float ratio = trackedEnemy.MaxHP > 0
            ? Mathf.Clamp01((float)trackedEnemy.currentHP / trackedEnemy.MaxHP)
            : 0f;

        fillRect.anchorMax = new Vector2(ratio, fillRect.anchorMax.y);

        // Theo vị trí world của enemy
        if (cam != null && trackedEnemy.SpawnedModel != null)
        {
            Vector3 world  = trackedEnemy.SpawnedModel.transform.position + worldOffset;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            rt.position    = screen;
        }

        gameObject.SetActive(trackedEnemy.IsAlive);
    }
}
