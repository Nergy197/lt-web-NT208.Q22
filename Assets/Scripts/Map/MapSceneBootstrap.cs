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
            new GameObject("GameManager").AddComponent<GameManager>();
            Debug.Log("[MapBootstrap] Tạo GameManager.");
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
                Debug.LogWarning("[MapBootstrap] Chưa gán questManagerPrefab.");
        }
    }

    void Start()
    {
        // Nếu không có quest nào active, bắt đầu quest mặc định
        if (QuestManager.Instance == null) return;

        if (QuestManager.Instance.ActiveQuests.Count == 0 &&
            QuestManager.Instance.CompletedQuests.Count == 0)
        {
            QuestManager.Instance.LoadProgress();   // load từ PlayerPrefs nếu có

            // Nếu vẫn không có → start quest mặc định
            if (QuestManager.Instance.ActiveQuests.Count == 0)
            {
                QuestManager.Instance.StartNewGame();
                Debug.Log("[Bootstrap] Đã kích hoạt StartingQuests.");
            }
        }
    }
}
