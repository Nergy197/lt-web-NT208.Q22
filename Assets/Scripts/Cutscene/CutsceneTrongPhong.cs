using UnityEngine;
using System.Collections;

/// <summary>
/// Điều phối cutscene TranQuocTuan vào phòng gặp cha (Chapter 1).
/// Gọi BatDauCutscene() từ trigger hoặc DaoDien_BaoTin để bắt đầu.
/// </summary>
public class CutsceneTrongPhong : MonoBehaviour
{
    [Header("References")]
    [Tooltip("PlayerMovement_Cutscene của TranQuocTuan")]
    public PlayerMovement_Cutscene playerScript;

    private bool inRange = false;
    private bool isTalking = false;

    private Animator     playerAnim;
    private Transform    playerTransform;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = true;
            Debug.Log("[CutsceneTrongPhong] Đã vào vùng tương tác, bấm F để nói chuyện.");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) inRange = false;
    }

    void Update()
    {
        if (!inRange || isTalking) return;
        
        bool pressed = false;
        if (InputController.Instance != null)
            pressed = InputController.Instance.Input.Map.Interact.WasPressedThisFrame();
        else
            pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return);

        if (pressed)
        {
            isTalking = true;
            BatDauGapChaNgay();
        }
    }

    public void BatDauCutscene()
    {
        // Phương thức này giờ không chạy tự động nữa, chờ người dùng qua Interact.
        Debug.Log("[CutsceneTrongPhong] Cửa không tự ép người chơi chạy nữa. Hãy tự đi đến gần giường!");
    }

    void BatDauGapChaNgay()
    {
        if (playerScript == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) playerScript = playerObj.GetComponent<PlayerMovement_Cutscene>();
        }

        if (playerScript != null)
        {
            playerScript.canMove = false;
            var rb = playerScript.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Ép player quay mặt lên giường (Vector2.up)
            playerAnim = playerScript.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetFloat("MoveX", 0f);
                playerAnim.SetFloat("MoveY", 1f);
                playerAnim.speed = 0f;
            }
        }

        var heThongThoai = GetComponent<QuanLyHoiThoai>();
        if (heThongThoai != null)
        {
            // Đăng ký mở khóa player sau khi nói chuyện xong
            heThongThoai.OnEnd += () => {
                isTalking = false;
                if (playerScript != null) playerScript.canMove = true;
            };
            heThongThoai.BatDauThoai();
        }
        else
        {
            Debug.LogWarning("[CutsceneTrongPhong] Không tìm thấy QuanLyHoiThoai!");
        }
    }
}
