using UnityEngine;

/// <summary>
/// Bootstrap cho BattleScene — đảm bảo các singleton tối thiểu tồn tại
/// khi mở thẳng BattleScene trong Editor mà không qua MapScene.
///
/// Trong build thực, GameManager/InputController đã được tạo từ StartScene
/// và DontDestroyOnLoad → bootstrap tự bỏ qua.
/// </summary>
public class BattleSceneBootstrap : MonoBehaviour
{
    [Header("Debug Enemy Spawn")]
    [Tooltip("MapData dùng để lấy possibleEnemies + enemyLevel khi test BattleScene trực tiếp.")]
    [SerializeField] private Mapdata debugMapData;

    [Tooltip("Số enemy tối thiểu spawn trong trận test.")]
    [SerializeField][Range(1, 5)] private int minEnemies = 1;

    [Tooltip("Số enemy tối đa spawn trong trận test.")]
    [SerializeField][Range(1, 5)] private int maxEnemies = 3;

    void Awake()
    {
        EnsureGameManager();
        EnsureInputController();
        EnsureMapManager();

        if (MapManager.Instance != null &&
            (MapManager.Instance.currentEnemies == null || MapManager.Instance.currentEnemies.Count == 0))
        {
            LoadDebugEnemies();
        }
    }

    void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
        Debug.Log("[BattleBootstrap] Tạo GameManager.");
    }

    void EnsureInputController()
    {
        if (InputController.Instance != null) return;
        new GameObject("InputController").AddComponent<InputController>();
        Debug.Log("[BattleBootstrap] Tạo InputController.");
    }

    void EnsureMapManager()
    {
        if (MapManager.Instance != null) return;
        new GameObject("MapManager").AddComponent<MapManager>();
        Debug.Log("[BattleBootstrap] Tạo MapManager.");
    }

    void LoadDebugEnemies()
    {
        if (debugMapData == null || debugMapData.possibleEnemies.Count == 0)
        {
            Debug.LogWarning("[BattleBootstrap] Chưa gán debugMapData — BattleManager sẽ dùng demo fallback.");
            return;
        }

        int count = UnityEngine.Random.Range(minEnemies, maxEnemies + 1);
        var picked = new System.Collections.Generic.List<EnemyData>();
        for (int i = 0; i < count; i++)
            picked.Add(debugMapData.GetRandomEnemy());

        MapManager.Instance.SetupBattle(picked, debugMapData.enemyLevel);
        MapManager.Instance.isInBattle = true;
        Debug.Log($"[BattleBootstrap] Spawn {count} enemy ngẫu nhiên từ '{debugMapData.mapName}' (level {debugMapData.enemyLevel}).");
    }
}
