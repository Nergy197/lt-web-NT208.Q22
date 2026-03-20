using UnityEngine;
using System.Collections;

public class CutsceneTrongPhong : MonoBehaviour
{
    public PlayerMovement playerScript;
    public Transform diemRe_Waypoint;
    public Transform diemCanhGiuong;
    public float tocDoChay = 5f;
    public GameObject khungThoaiUI;

    private Animator playerAnim;
    private Transform playerTransform; // Dùng riêng để di chuyển, tránh null

    public void BatDauCutscene()
    {
        // Tự động tìm Player nếu chưa gán trong Inspector
        if (playerScript == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerScript = playerObj.GetComponent<PlayerMovement>();
                playerTransform = playerObj.transform;
                playerAnim = playerObj.GetComponent<Animator>();
                Debug.Log("[CutsceneTrongPhong] Tự tìm Player bằng Tag thành công!");
            }
            else
            {
                Debug.LogError("[CutsceneTrongPhong] Không tìm thấy GameObject nào có Tag 'Player'!");
                return;
            }
        }
        else
        {
            playerTransform = playerScript.transform;
            playerAnim = playerScript.GetComponent<Animator>();
        }

        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);
        StartCoroutine(KichBanGapCha());
    }

    IEnumerator KichBanGapCha()
    {
        // 1. RÚT ĐIỆN: Tắt Script di chuyển + dừng vật lý
        if (playerScript != null)
        {
            playerScript.enabled = false;
            var rb = playerScript.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // Kiểm tra waypoints
        if (playerTransform == null || diemRe_Waypoint == null || diemCanhGiuong == null)
        {
            Debug.LogError("[CutsceneTrongPhong] Thiếu playerTransform hoặc Waypoint! Hãy gán trong Inspector.");
            yield break;
        }

        // ==========================================
        // NHỊP 1: Chạy Lên
        // ==========================================
        if (playerAnim != null)
        {
            playerAnim.SetFloat("MoveX", 0f);
            playerAnim.SetFloat("MoveY", 1f);
            playerAnim.speed = 1f;
        }

        while (Vector3.Distance(playerTransform.position, diemRe_Waypoint.position) > 0.1f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, diemRe_Waypoint.position, tocDoChay * Time.deltaTime);
            yield return null;
        }

        // ==========================================
        // NHỊP 2: Rẽ Trái đến giường
        // ==========================================
        if (playerAnim != null)
        {
            playerAnim.SetFloat("MoveX", -1f);
            playerAnim.SetFloat("MoveY", 0f);
        }

        while (Vector3.Distance(playerTransform.position, diemCanhGiuong.position) > 0.1f)
        {
            playerTransform.position = Vector3.MoveTowards(playerTransform.position, diemCanhGiuong.position, tocDoChay * Time.deltaTime);
            yield return null;
        }

        // ==========================================
        // TỚI NƠI: Dừng chạy, đứng im nghiêm chỉnh
        // ==========================================
        if (playerAnim != null)
        {
            playerAnim.SetFloat("MoveX", -1f);
            playerAnim.SetFloat("MoveY", 0f);
            yield return null;
            playerAnim.speed = 0f;
        }

        // Đợi nửa giây cho cảm xúc lắng đọng rồi bật hội thoại
        yield return new WaitForSeconds(0.5f);

        // Gọi QuanLyHoiThoai trên cùng Object này
        QuanLyHoiThoai heThongThoai = GetComponent<QuanLyHoiThoai>();
        if (heThongThoai != null)
        {
            heThongThoai.BatDauThoai();
        }
    }
}