using UnityEngine;

/// <summary>
/// Gắn script này lên bất kỳ GameObject nào trên Map để biến nó thành điểm Save Point.
/// Đặt pointId khác nhau cho từng điểm trong Inspector.
/// </summary>
public class SavePoint : MonoBehaviour
{
    [Tooltip("ID duy nhất của điểm lưu này. VD: 'village_inn', 'forest_camp'")]
    public string pointId = "default";

    private bool isPlayerInRange = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
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
        }
    }

    private void Update()
    {
        if (!isPlayerInRange) return;
        if (InputController.Instance == null) return;

        // Chỉ xử lý khi đang ở chế độ Map (tránh xung đột phím F với Battle/UI khác)
        if (InputController.Instance.Mode != InputMode.Map) return;

        // Nhấn phím F (Interact) để mở Menu Party
        if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
        {
            SavePointUI.Instance?.Open(this);
        }
    }
}
