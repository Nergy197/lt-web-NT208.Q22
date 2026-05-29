using UnityEngine;

/// <summary>
/// Quản lý background cho Chapter5a_Battle tại runtime.
///
/// Hành vi:
/// - Nếu <see cref="MapManager.Instance.currentMap.battleBackgroundPrefab"/> được set,
///   instantiate prefab đó dưới <see cref="backgroundParent"/>.
/// - Nếu null hoặc MapManager không tồn tại, fallback về <see cref="defaultBackgroundPrefab"/>
///   (gán trong Inspector — thường là background copy từ Chapter2_Tutorial).
/// - Nếu Chapter5a_Battle đã có sẵn background baked (ví dụ TutorialCopy_* root từ
///   editor tool), giữ nguyên nếu <see cref="preserveSceneBaked"/> bật và không
///   có prefab nào để spawn.
///
/// Gắn component này vào một GameObject trống trong Chapter5a_Battle (ví dụ
/// "BattleBackground" parent ở root). Sau này khi map data có prefab riêng,
/// background sẽ tự đổi mà không cần chỉnh Chapter5a_Battle.
/// </summary>
public class BattleBackgroundController : MonoBehaviour
{
    [Header("Background Source")]
    [Tooltip("Prefab fallback khi map data chưa có battleBackgroundPrefab.\n" +
             "Tạm thời dùng prefab dựng từ visual roots của Chapter2_Tutorial.")]
    [SerializeField] private GameObject defaultBackgroundPrefab;

    [Tooltip("Parent để chứa background instance. Để null sẽ dùng chính transform của component này.")]
    [SerializeField] private Transform backgroundParent;

    [Tooltip("Nếu Chapter5a_Battle đã có background baked sẵn (ví dụ TutorialCopy_* root) " +
             "và không có prefab nào cấu hình, giữ nguyên thay vì xoá.")]
    [SerializeField] private bool preserveSceneBaked = true;

    [Header("Layout")]
    [Tooltip("Local position được áp dụng cho instance background.")]
    [SerializeField] private Vector3 spawnLocalPosition = Vector3.zero;

    [Tooltip("Local scale áp dụng cho instance. Để (1,1,1) nếu prefab đã có scale chuẩn.")]
    [SerializeField] private Vector3 spawnLocalScale = Vector3.one;

    private GameObject spawnedBackground;

    void Awake()
    {
        if (backgroundParent == null) backgroundParent = transform;
        ApplyBackground();
    }

    /// <summary>
    /// Tải background dựa vào map hiện tại. Gọi lại nếu cần đổi background runtime.
    /// </summary>
    public void ApplyBackground()
    {
        GameObject prefab = ResolvePrefab();

        if (prefab == null)
        {
            if (preserveSceneBaked)
            {
                Debug.Log("[BattleBackground] Không có prefab cấu hình, giữ background baked sẵn trong scene.");
                return;
            }
            Debug.LogWarning("[BattleBackground] Không có prefab và không giữ baked — scene sẽ trống.");
            return;
        }

        if (spawnedBackground != null)
        {
            Destroy(spawnedBackground);
            spawnedBackground = null;
        }

        spawnedBackground = Instantiate(prefab, backgroundParent);
        spawnedBackground.transform.localPosition = spawnLocalPosition;
        spawnedBackground.transform.localScale = spawnLocalScale;
        spawnedBackground.name = $"Battle_Background ({prefab.name})";
        Debug.Log($"[BattleBackground] Spawned: {spawnedBackground.name}");
    }

    private GameObject ResolvePrefab()
    {
        // Ưu tiên 1: prefab khai báo trên Mapdata hiện tại.
        if (MapManager.Instance != null &&
            MapManager.Instance.currentMap != null &&
            MapManager.Instance.currentMap.battleBackgroundPrefab != null)
        {
            return MapManager.Instance.currentMap.battleBackgroundPrefab;
        }

        // Ưu tiên 2: prefab fallback gán trong Inspector của controller.
        return defaultBackgroundPrefab;
    }
}
