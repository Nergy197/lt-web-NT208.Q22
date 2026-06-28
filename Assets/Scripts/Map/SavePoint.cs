using UnityEngine;

/// <summary>
/// Gắn script này lên bất kỳ GameObject nào trên Map để biến nó thành điểm Save Point.
/// Đặt pointId khác nhau cho từng điểm trong Inspector.
/// </summary>
public class SavePoint : MonoBehaviour
{
    [Tooltip("ID duy nhất của điểm lưu này. VD: 'village_inn', 'forest_camp'")]
    public string pointId = "default";

    [Header("Phát hiện theo khoảng cách (cho điểm tạo runtime / web)")]
    [Tooltip("Bật để phát hiện player bằng khoảng cách thay vì trigger collider — hoạt động kể cả khi đặt ngay chỗ player spawn.")]
    public bool useDistanceDetection = false;
    [Tooltip("Bán kính (world units) coi là player đang đứng trong điểm save.")]
    public float detectionRadius = 1.6f;

    private bool isPlayerInRange = false;
    private Transform _playerTf;

    private bool PlayerInRange()
    {
        if (!useDistanceDetection) return isPlayerInRange;

        // Distance-based: tự tìm player theo tag, không cần trigger collider.
        if (_playerTf == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) _playerTf = p.transform;
        }
        if (_playerTf == null) return false;

        float dx = _playerTf.position.x - transform.position.x;
        float dy = _playerTf.position.y - transform.position.y;
        bool inRange = (dx * dx + dy * dy) <= detectionRadius * detectionRadius;

        // Cập nhật nút Interact mobile theo trạng thái.
        if (inRange != isPlayerInRange)
        {
            isPlayerInRange = inRange;
            MobileInteractRegistry.SetActive(this, inRange);
        }
        return inRange;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            MobileInteractRegistry.SetActive(this, true);
            Debug.Log($"[SavePoint] Player dẫm vào {pointId}. Nhấn F để mở Menu Lưu/Hồi máu.");
            
            // Xóa tính năng tự động Lưu và Hồi máu ở đây.
            // Bắt buộc người chơi phải bấm Interact (F) để mở bảng UI.
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            MobileInteractRegistry.SetActive(this, false);
        }
    }

    private void OnDisable()
    {
        MobileInteractRegistry.SetActive(this, false);
    }

    private void Update()
    {
        if (!PlayerInRange()) return;
        if (InputController.Instance == null) return;

        // Chỉ xử lý khi đang ở chế độ Map (tránh xung đột phím F với Battle/UI khác)
        if (InputController.Instance.Mode != InputMode.Map) return;

        // Nhấn phím F (Interact) để mở Menu Party
        if (InputController.Instance.IsInteractPressed())
        {
            SavePointUI.Instance?.Open(this);
        }
    }
}
