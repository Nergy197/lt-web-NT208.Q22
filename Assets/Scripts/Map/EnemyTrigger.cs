using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyTrigger : MonoBehaviour
{
    public EnemyData enemyData;   // enemy sẽ đánh
    public int mapLevel = 1;      // level map

    [Tooltip("Check nếu muốn tới gần mớt bấm F đánh (như NPC hoặc boss đứng im). Tắt đi nếu muốn đâm sầm vào là bị đánh ngay (quái chạy trên đường).")]
    public bool requireInteract = false;

    private bool triggered = false;
    private bool isPlayerInRange = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (requireInteract)
        {
            isPlayerInRange = true;
            Debug.Log($"[EnemyTrigger] Nhấn F để chiến đấu với ({gameObject.name})");
        }
        else
        {
            StartBattle();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
    }

    private void Update()
    {
        if (triggered) return;

        if (requireInteract && isPlayerInRange && InputController.Instance != null)
        {
            if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
            {
                StartBattle();
            }
        }
    }

    private void StartBattle()
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("[EnemyTrigger] MapManager.Instance is null!");
            return;
        }

        if (enemyData == null)
        {
            Debug.LogError("[EnemyTrigger] enemyData chưa được gán trong Inspector!");
            return;
        }

        triggered = true;

        // Thiết lập enemy cố định cho trận này
        MapManager.Instance.currentEnemies.Clear();
        MapManager.Instance.currentEnemies.Add(enemyData);
        MapManager.Instance.currentMapLevel = mapLevel;

        Debug.Log($"[EnemyTrigger] Starting battle with {enemyData.entityName} (Lv {mapLevel})");
        MapManager.Instance.StartBattle();
    }
}
