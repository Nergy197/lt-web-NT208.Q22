using UnityEngine;

/// <summary>
/// Bootstrap cho BattleScene — đảm bảo các runtime singleton tối thiểu tồn tại
/// khi developer mở thẳng BattleScene trong Editor (không đi qua MapScene).
///
/// Trong build thực tế, các singleton này đã được tạo từ StartScene/MapScene
/// và DontDestroyOnLoad nên bootstrap sẽ tự huỷ.
///
/// Gắn component này vào một GameObject "BattleSceneBootstrap" trong BattleScene.
/// </summary>
public class BattleSceneBootstrap : MonoBehaviour
{
    [Header("Editor-only fallbacks")]
    [Tooltip("Prefab GameManager dùng khi mở thẳng BattleScene (test). Có thể để trống nếu đã chạy từ StartScene/MapScene.")]
    [SerializeField] private GameManager gameManagerPrefab;

    [Tooltip("Prefab InputController dùng khi mở thẳng BattleScene (test).")]
    [SerializeField] private InputController inputControllerPrefab;

    [Tooltip("Prefab MapManager dùng khi mở thẳng BattleScene (test) — cần để có currentEnemies giả lập.")]
    [SerializeField] private MapManager mapManagerPrefab;

    [Tooltip("Test enemy list khi mở thẳng BattleScene (chỉ dùng nếu MapManager mới được spawn).")]
    [SerializeField] private System.Collections.Generic.List<EnemyData> debugEnemies = new System.Collections.Generic.List<EnemyData>();

    [Tooltip("Test map level khi mở thẳng BattleScene.")]
    [SerializeField] private int debugMapLevel = 1;

    void Awake()
    {
        EnsureGameManager();
        EnsureInputController();
        EnsureMapManager();

        // Sau khi đã có MapManager, nếu chưa có enemy nào (vì không qua MapScene),
        // nạp tạm danh sách debug để BattleManager có dữ liệu mà chạy.
        if (MapManager.Instance != null &&
            (MapManager.Instance.currentEnemies == null || MapManager.Instance.currentEnemies.Count == 0) &&
            debugEnemies != null && debugEnemies.Count > 0)
        {
            MapManager.Instance.currentEnemies = new System.Collections.Generic.List<EnemyData>(debugEnemies);
            MapManager.Instance.currentMapLevel = Mathf.Max(1, debugMapLevel);
            MapManager.Instance.isInBattle = true;
            Debug.Log($"[BattleBootstrap] Đã nạp {debugEnemies.Count} enemy debug để test BattleScene.");
        }
    }

    void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;
        if (gameManagerPrefab != null)
        {
            var gm = Instantiate(gameManagerPrefab);
            gm.name = "GameManager";
            Debug.Log("[BattleBootstrap] Tạo GameManager cứu hộ.");
        }
        else
        {
            Debug.LogWarning("[BattleBootstrap] Thiếu GameManager prefab — battle có thể không hoạt động khi test.");
        }
    }

    void EnsureInputController()
    {
        if (InputController.Instance != null) return;
        if (inputControllerPrefab != null)
        {
            var ic = Instantiate(inputControllerPrefab);
            ic.name = "InputController";
            Debug.Log("[BattleBootstrap] Tạo InputController cứu hộ.");
        }
        else
        {
            new GameObject("InputController").AddComponent<InputController>();
            Debug.Log("[BattleBootstrap] Tạo InputController mặc định (không có prefab).");
        }
    }

    void EnsureMapManager()
    {
        if (MapManager.Instance != null) return;
        if (mapManagerPrefab != null)
        {
            var mm = Instantiate(mapManagerPrefab);
            mm.name = "MapManager";
            Debug.Log("[BattleBootstrap] Tạo MapManager cứu hộ.");
        }
        else
        {
            new GameObject("MapManager").AddComponent<MapManager>();
            Debug.Log("[BattleBootstrap] Tạo MapManager mặc định (không có prefab).");
        }
    }
}
