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
            Debug.Log($"[SavePoint] Player đẫm vào Save Point. Tự động Hồi máu & Lưu game...");
            
            // Tự động Hồi máu và Lưu khi chạm vào
            if (GameManager.Instance != null)
            {
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                GameManager.Instance.SaveAtPoint(pointId, sceneName);
            }
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
        if (isPlayerInRange && InputController.Instance != null)
        {
            // Nhấn phím F (Interact) để mở Menu Party
            if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
            {
                SavePointUI.Instance?.Open(this);
            }
        }
    }
}
