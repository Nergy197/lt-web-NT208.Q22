using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Trụ dịch chuyển kiểu Fast-Travel.
/// Mỗi trụ VỪA LÀ điểm xuất hiện (spawn) VỪA LÀ nơi tương tác mở menu.
/// Các trụ tự tìm nhau trong scene tạo thành mạng lưới dịch chuyển.
///
/// Player đến gần trụ → nhấn F → menu hiện danh sách các trụ khác → chọn → teleport tới trụ đó.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TeleportPillar : MonoBehaviour
{
    [Header("Pillar Info")]
    [Tooltip("Tên hiển thị trên menu (vd: 'Làng Khởi Đầu', 'Rừng Sâu').")]
    public string pillarName = "Trụ Dịch Chuyển";

    [Tooltip("Mô tả ngắn hiển thị khi hover trên menu.")]
    [TextArea(1, 3)]
    public string description = "";

    [Header("Map Data (Tùy chọn)")]
    [Tooltip("Mapdata khu vực này. Khi teleport tới đây → MapManager sẽ đổi dữ liệu quái/encounter.")]
    public Mapdata mapData;

    [Header("Spawn Offset")]
    [Tooltip("Khoảng lệch player xuất hiện so với trụ (tránh chồng collider). Mặc định: hơi dưới trụ.")]
    public Vector2 spawnOffset = new Vector2(0, -1.5f);

    [Header("Minimap Icon")]
    [Tooltip("Hiển thị icon trên minimap.")]
    public bool showOnMinimap = true;

    [Tooltip("Màu icon trên minimap.")]
    public Color minimapIconColor = new Color(0.4f, 0.3f, 0.9f, 1f);

    [Tooltip("Kích thước icon trên minimap.")]
    public float minimapIconSize = 1.5f;

    // Vị trí spawn thực tế (tính từ position + offset)
    public Vector3 SpawnPosition => transform.position + (Vector3)spawnOffset;

    private bool isPlayerInRange = false;

    // ================= STATIC REGISTRY =================
    // Tất cả các trụ trong scene tự đăng ký vào đây
    private static List<TeleportPillar> allPillars = new List<TeleportPillar>();

    /// <summary>Trả về danh sách tất cả trụ đang active trong scene.</summary>
    public static List<TeleportPillar> GetAllPillars() => allPillars;

    // ================= LIFECYCLE =================

    private void Awake()
    {
        // Minimap icon
        if (showOnMinimap && GetComponent<MinimapIcon>() == null)
        {
            MinimapIcon icon = gameObject.AddComponent<MinimapIcon>();
            icon.iconColor = minimapIconColor;
            icon.iconSize = minimapIconSize;
            icon.showLabel = true;
            icon.labelText = pillarName;
        }
    }

    private void OnEnable()
    {
        if (!allPillars.Contains(this))
            allPillars.Add(this);
    }

    private void OnDisable()
    {
        allPillars.Remove(this);
    }

    private void Reset()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    // ================= INTERACTION =================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (InputController.Instance != null &&
            InputController.Instance.Mode != InputMode.Map) return;

        isPlayerInRange = true;
        Debug.Log($"[TeleportPillar] Nhấn F để mở menu dịch chuyển ({pillarName})");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
    }

    private void Update()
    {
        if (!isPlayerInRange) return;
        if (InputController.Instance == null) return;
        if (InputController.Instance.Mode != InputMode.Map) return;

        if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
        {
            OpenMenu();
        }
    }

    private void OpenMenu()
    {
        if (TeleportMenuUI.Instance == null)
        {
            Debug.LogError("[TeleportPillar] Không tìm thấy TeleportMenuUI trong scene! " +
                           "Hãy chạy Tools/Teleport Menu/1. Tạo Teleport Menu UI Tự Động");
            return;
        }

        // Lấy danh sách trụ khác (loại trừ chính mình)
        List<TeleportPillar> destinations = new List<TeleportPillar>();
        foreach (var pillar in allPillars)
        {
            if (pillar != this && pillar != null && pillar.gameObject.activeInHierarchy)
            {
                destinations.Add(pillar);
            }
        }

        if (destinations.Count == 0)
        {
            Debug.LogWarning($"[TeleportPillar] {pillarName}: Không có trụ nào khác trong scene để dịch chuyển tới!");
            return;
        }

        TeleportMenuUI.Instance.Open(this, destinations);
    }

    // ================= GIZMO (EDITOR) =================

    private void OnDrawGizmosSelected()
    {
        // Vẽ vị trí spawn
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(SpawnPosition, 0.3f);
        Gizmos.DrawLine(transform.position, SpawnPosition);
    }
}
