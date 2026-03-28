using UnityEngine;

/// <summary>
/// Bootstrapper cho MapScene — khởi động QuestManager và GameManager
/// khi test trực tiếp từ MapScene (không đi qua StartScene).
///
/// Gắn vào một GameObject rỗng tên "Bootstrap" trong MapScene.
/// Component này tự xóa mình trong build thực tế (khi GameManager đã tồn tại).
/// </summary>
public class MapSceneBootstrap : MonoBehaviour
{
    [Header("Chỉ dùng khi test trực tiếp MapScene")]
    [Tooltip("Kéo QuestManager prefab/GameObject vào đây")]
    public QuestManager questManagerPrefab;

    [Tooltip("Kéo InputController prefab/GameObject vào đây")]
    public InputController inputControllerPrefab;

    void Awake()
    {
        // 1. Tự động tạo InputController nếu thiếu (cứu lỗi không di chuyển được)
        if (InputController.Instance == null && inputControllerPrefab != null)
        {
            var ic = Instantiate(inputControllerPrefab);
            ic.name = "InputController";
            Debug.Log("[Bootstrap] Đã tạo InputController cứu hộ.");
        }

        // 2. Nếu đã có QuestManager (từ StartScene), không làm gì thêm
        if (QuestManager.Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Spawn QuestManager nếu chưa có (chỉ khi test trực tiếp)
        if (questManagerPrefab != null)
        {
            var qm = Instantiate(questManagerPrefab);
            qm.name = "QuestManager";
            Debug.Log("[Bootstrap] Đã tạo QuestManager cho MapScene test.");
        }
        else
        {
            Debug.LogWarning("[Bootstrap] Không tìm thấy QuestManager prefab! " +
                             "Kéo prefab vào field 'Quest Manager Prefab' của Bootstrap.");
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
