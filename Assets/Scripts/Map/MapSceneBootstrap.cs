using UnityEngine;

/// <summary>
/// Bootstrapper cho MapScene — khởi động các singleton cần thiết
/// khi test trực tiếp từ MapScene (không đi qua StartScene).
/// Gắn vào một GameObject rỗng tên "Bootstrap" trong MapScene.
/// </summary>
public class MapSceneBootstrap : MonoBehaviour
{
    [Header("Chỉ dùng khi test trực tiếp MapScene")]
    [Tooltip("Kéo QuestManager prefab/GameObject vào đây")]
    public QuestManager questManagerPrefab;

    [Tooltip("(Tùy chọn) Prefab GameManager đã gán playerDatabase. Để trống sẽ tự tạo và nạp database.")]
    public GameManager gameManagerPrefab;

    [Tooltip("Kéo InputController prefab/GameObject vào đây")]
    public InputController inputControllerPrefab;

    [Tooltip("Mapdata của vùng xuất phát để random encounter hoạt động ngay khi test")]
    public Mapdata debugMapData;

    void Awake()
    {
        // 1. InputController
        if (InputController.Instance == null)
        {
            if (inputControllerPrefab != null)
                Instantiate(inputControllerPrefab).name = "InputController";
            else
                new GameObject("InputController").AddComponent<InputController>();
            Debug.Log("[MapBootstrap] Tạo InputController.");
        }

        // 2. GameManager
        if (GameManager.Instance == null)
        {
            if (gameManagerPrefab != null)
                Instantiate(gameManagerPrefab).name = "GameManager";
            else
            {
                GameManager gm = new GameObject("GameManager").AddComponent<GameManager>();
                gm.EnsurePlayerDatabase();
            }
            Debug.Log("[MapBootstrap] Tạo GameManager.");
        }
        else if (GameManager.Instance.playerDatabase == null || GameManager.Instance.playerDatabase.Count == 0)
        {
            GameManager.Instance.EnsurePlayerDatabase();
        }

        // 3. MapManager
        if (MapManager.Instance == null)
        {
            var mm = new GameObject("MapManager").AddComponent<MapManager>();
            if (debugMapData != null)
                mm.defaultMap = debugMapData;
            Debug.Log("[MapBootstrap] Tạo MapManager.");
        }
        else if (MapManager.Instance.currentMap == null && debugMapData != null)
        {
            MapManager.Instance.SetMap(debugMapData);
        }

        // 4. QuestManager
        if (QuestManager.Instance == null)
        {
            if (questManagerPrefab != null)
                Instantiate(questManagerPrefab).name = "QuestManager";
            else
            {
                new GameObject("QuestManager").AddComponent<QuestManager>();
                Debug.LogWarning("[MapBootstrap] Chưa gán questManagerPrefab — đã tạo QuestManager rỗng (quest có thể thiếu). Nên chạy từ StartScene hoặc gán prefab.");
            }
        }
    }

    void Start()
    {
        QuestMapUIEnsurer.EnsureTrackerUnderMinimap();

        if (QuestManager.Instance == null) return;

        var qm = QuestManager.Instance;
        if (!qm.HasAnyProgress())
            qm.LoadProgress();

        qm.TryStartChapter1Quests();

        var tracker = Object.FindAnyObjectByType<QuestTrackerUnderMinimapUI>();
        if (tracker != null) tracker.RefreshUI();
    }
}
