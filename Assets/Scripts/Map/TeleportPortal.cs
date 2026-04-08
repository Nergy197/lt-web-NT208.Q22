using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TeleportPortal : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("Vị trí (chuẩn hoặc rỗng) mà Player sẽ dịch chuyển tới.")]
    public Transform destination;

    [Tooltip("Bật nếu cần Player nhấn phím tương tác (vd: F) để qua cổng. Tắt để tự động qua cổng khi chạm vào.")]
    public bool requireInteract = false;
    
    [Header("Map Settings (Tùy chọn)")]
    [Tooltip("Cho phép thay đổi logic zone hiện tại (Quái vật, Encounter Rate...) khi portal.\nKéo Mapdata vào đây nếu cổng này dẫn thẳng sang khu vực khác.")]
    public Mapdata targetMapData;

    private bool isPlayerInRange = false;
    
    // Static cooldown ngắn hạn (0.5s) để ngăn lặp vô tận nếu destination rơi trúng ngay portal ngược lại
    private static float lastTeleportTime = 0f;

    private void Reset()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    // ================= CONFLICT GUARD =================
    // Nếu InputController đang ở chế độ UI (SavePointUI đang mở) thì chặn mọi thao tác teleport.
    private bool IsInputBlocked()
    {
        if (InputController.Instance == null) return false;
        return InputController.Instance.Mode == InputMode.UI
            || InputController.Instance.Mode == InputMode.Battle
            || InputController.Instance.Mode == InputMode.BattleSkillMenu;
    }

    // Kiểm tra xem có SavePoint nào đang overlap cùng vị trí Player hay không.
    // Nếu có → nhường quyền ưu tiên phím F cho SavePoint.
    private bool IsSavePointOverlapping(Collider2D playerCollider)
    {
        if (playerCollider == null) return false;

        // Tìm tất cả SavePoint trong cảnh
        SavePoint[] savePoints = Object.FindObjectsByType<SavePoint>(FindObjectsSortMode.None);
        foreach (var sp in savePoints)
        {
            Collider2D spCol = sp.GetComponent<Collider2D>();
            if (spCol != null && spCol.bounds.Intersects(playerCollider.bounds))
            {
                return true;
            }
        }
        return false;
    }

    // ================= TRIGGER =================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (IsInputBlocked()) return;

        if (requireInteract)
        {
            isPlayerInRange = true;
            Debug.Log($"[TeleportPortal] Nhấn phím tương tác để qua cổng ({gameObject.name})");
        }
        else
        {
            Teleport(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
    }

    private void Update()
    {
        if (!requireInteract) return;
        if (!isPlayerInRange) return;
        if (IsInputBlocked()) return;
        if (InputController.Instance == null) return;

        if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
        {
            // Nếu có SavePoint chồng lên vùng này → nhường phím F cho SavePoint, không teleport.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Collider2D playerCol = player.GetComponent<Collider2D>();
                if (IsSavePointOverlapping(playerCol))
                {
                    Debug.Log($"[TeleportPortal] Nhường quyền Interact cho SavePoint (chồng vùng).");
                    return;
                }

                Teleport(player.transform);
            }
        }
    }

    // ================= TELEPORT CORE =================

    private void Teleport(Transform playerTransform)
    {
        // Kiểm tra delay (0.5 giây) chống lặp
        if (Time.time - lastTeleportTime < 0.5f) return;

        // Chặn khi UI đang mở
        if (IsInputBlocked()) return;
        
        if (destination == null)
        {
            Debug.LogWarning($"[TeleportPortal] {gameObject.name}: Chưa thiết lập vị trí Destination!");
            return;
        }

        // Cập nhật vị trí
        playerTransform.position = destination.position;
        lastTeleportTime = Time.time;
        Debug.Log($"[TeleportPortal] Dịch chuyển Player tới {destination.name}");

        // Cập nhật MapData hiện tại qua MapManager nếu có và nếu map mới khác map cũ
        if (targetMapData != null && MapManager.Instance != null && MapManager.Instance.currentMap != targetMapData)
        {
            MapManager.Instance.SetMap(targetMapData);
        }
    }
}
