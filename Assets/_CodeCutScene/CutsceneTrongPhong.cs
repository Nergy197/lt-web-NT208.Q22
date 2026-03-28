using UnityEngine;
using System.Collections;

public class CutsceneTrongPhong : MonoBehaviour
{
    public PlayerMovement_Cutscene playerScript;
    public Transform diemRe_Waypoint;
    public Transform diemCanhGiuong;
    public float tocDoChay = 5f;
    public GameObject khungThoaiUI;

    private Animator playerAnim;

    public void BatDauCutscene()
    {
        if (playerScript != null) playerAnim = playerScript.GetComponent<Animator>();
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);
        StartCoroutine(KichBanGapCha());
    }

    IEnumerator KichBanGapCha()
    {
        // 1. RÚT ĐIỆN: Tắt hẳn Script di chuyển để nó không chèn ép Animator
        if (playerScript != null)
        {
            playerScript.canMove = false;
            playerScript.enabled = false; // Dòng quyền lực nhất là đây!
        }

        // ==========================================
        // NHỊP 1: Chạy Lên
        // ==========================================
        if (playerAnim != null)
        {
            playerAnim.SetFloat("MoveX", 0f);
            playerAnim.SetFloat("MoveY", 1f);
            playerAnim.speed = 1f; // Ép phát hoạt ảnh chạy
        }

        while (Vector3.Distance(playerScript.transform.position, diemRe_Waypoint.position) > 0.1f)
        {
            playerScript.transform.position = Vector3.MoveTowards(playerScript.transform.position, diemRe_Waypoint.position, tocDoChay * Time.deltaTime);
            yield return null;
        }

        // ==========================================
        // NHỊP 2: Rẽ Trái đến giường
        // ==========================================
        if (playerAnim != null)
        {
            playerAnim.SetFloat("MoveX", -1f);
            playerAnim.SetFloat("MoveY", 0f);
            // Vẫn giữ anim.speed = 1f để tiếp tục nhịp chạy
        }

        while (Vector3.Distance(playerScript.transform.position, diemCanhGiuong.position) > 0.1f)
        {
            playerScript.transform.position = Vector3.MoveTowards(playerScript.transform.position, diemCanhGiuong.position, tocDoChay * Time.deltaTime);
            yield return null;
        }

        // ==========================================
        // TỚI NƠI: Dừng chạy, đứng im nghiêm chỉnh
        // ==========================================
        if (playerAnim != null) playerAnim.speed = 0f;

        // Đợi nửa giây cho cảm xúc lắng đọng rồi bật hội thoại
        yield return new WaitForSeconds(0.5f);

        // Gọi thẳng cái QuanLyHoiThoai dính trên cùng Object này để bắt đầu chạy chữ
        QuanLyHoiThoai heThongThoai = GetComponent<QuanLyHoiThoai>();
        if (heThongThoai != null)
        {
            heThongThoai.BatDauThoai();
        }
    }
}