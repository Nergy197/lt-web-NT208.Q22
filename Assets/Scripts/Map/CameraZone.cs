using UnityEngine;
using Unity.Cinemachine; 

public class CameraZone : MonoBehaviour
{
    // Kéo CM vcam1 vào đây
    public CinemachineConfiner2D confiner; 
    private Collider2D zoneCollider;

    void Start()
    {
        // Lấy chính cái Collider (khung xanh) của đối tượng này
        zoneCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nếu Player đi vào vùng này
        if (collision.CompareTag("Player"))
        {
            // Đổi khung giới hạn của camera sang vùng này
            confiner.BoundingShape2D = zoneCollider;
            // Xóa bộ nhớ đệm để camera cập nhật ngay lập tức
            confiner.InvalidateBoundingShapeCache();
        }
    }
}