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
        Debug.Log("TRIGGER BATTLE WORKED - Đang tiến hành chuyển Scene nếu Implement...");
        triggered = true;
        
        // TODO: Chèn logic GameManager vào BattleScene với enemyData được truyền qua đây.
        // Hiện tại MapManager đang quản lý random encounter, nếu EnemyTrigger được xài,
        // Cần gọi MapManager.Instance.StartFixedEncounter(enemyData, mapLevel); => nếu mapManager có sẵn.
    }
}
