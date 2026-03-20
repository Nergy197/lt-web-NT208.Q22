using UnityEngine;
using System.Collections;

public class QuanLyBaoTin : MonoBehaviour
{
    public PlayerMovement playerScript; // Kéo Quốc Tuấn vào đây
    public GameObject khungThoai;       // Kéo cái Bong bóng thoại vào đây

    void Start()
    {
        // Bắt đầu vở kịch ngay khi game chạy (hoặc anh có thể gọi hàm này sau)
        StartCoroutine(KichBanBaoTin());
    }

    IEnumerator KichBanBaoTin()
    {
        // Tự động tìm Player nếu chưa gán trong Inspector
        if (playerScript == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerScript = playerObj.GetComponent<PlayerMovement>();
        }

        // 1. KHÓA CHÂN
        if (playerScript != null)
        {
            playerScript.enabled = false;
            var rb = playerScript.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 2. CHỜ GIA NÔ CHẠY RA: Đợi khoảng 1 giây cho Gia nô chạy tới nơi
        yield return new WaitForSeconds(1.0f);

        // 3. HIỆN CHỮ: Bật khung thoại lên để bắt đầu "Lão gia đang nguy kịch..."
        if (khungThoai != null) khungThoai.SetActive(true);
    }
}