using UnityEngine;

public class TeleportDoor : MonoBehaviour
{
    public Transform diemXuatHien;
    public CutsceneTrongPhong kichBanPhongNgu;

    [Header("Cài đặt Camera")]
    [Tooltip("Kéo điểm chính giữa phòng vào đây. Nếu để trống, Camera sẽ đi theo nhân vật.")]
    public Transform viTriCamera;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Dịch chuyển nhân vật an toàn qua physics
            var rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.position = diemXuatHien.position;
            else
                other.transform.position = diemXuatHien.position;

            // 2. Dịch chuyển Camera
            if (Camera.main != null)
            {
                if (viTriCamera != null)
                {
                    // Nếu anh có cắm tâm phòng, ép Camera ra thẳng giữa phòng
                    Camera.main.transform.position = new Vector3(
                        viTriCamera.position.x,
                        viTriCamera.position.y,
                        Camera.main.transform.position.z
                    );
                }
                else
                {
                    // Nếu anh để trống, Camera bám theo nhân vật ở mép cửa
                    Camera.main.transform.position = new Vector3(
                        diemXuatHien.position.x,
                        diemXuatHien.position.y,
                        Camera.main.transform.position.z
                    );
                }
            }
        }
    }
}