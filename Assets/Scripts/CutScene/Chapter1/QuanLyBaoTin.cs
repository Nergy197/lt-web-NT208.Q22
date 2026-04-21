using UnityEngine;
using System.Collections;

public class QuanLyBaoTin : MonoBehaviour
{
    public PlayerMovement_Cutscene playerScript; // Kéo Quốc Tuấn vào đây
    public GameObject khungThoai;       // Kéo cái Bong bóng thoại vào đây

    void Start()
    {
        // Bắt đầu vở kịch ngay khi game chạy (hoặc anh có thể gọi hàm này sau)
        StartCoroutine(KichBanBaoTin());
    }

    IEnumerator KichBanBaoTin()
    {
        // 1. KHÓA CHÂN: Gọi cái công tắc canMove bên PlayerMovement_Cutscene
        if (playerScript != null) playerScript.canMove = false;

        // 2. CHỜ GIA NÔ CHẠY RA: Đợi khoảng 1 giây cho Gia nô chạy tới nơi
        yield return new WaitForSeconds(1.0f);

        // 3. HIỆN CHỮ: Bật khung thoại lên để bắt đầu "Lão gia đang nguy kịch..."
        if (khungThoai != null) khungThoai.SetActive(true);
    }
}