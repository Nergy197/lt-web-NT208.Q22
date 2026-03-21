using UnityEngine;
using System.Collections;

/// <summary>
/// Điều phối cutscene Gia Nô báo tin tại Chapter 1.
///
/// Luồng:
///   1. Gia Nô chạy từ cửa nhà → vị trí báo tin
///   2. QuanLyHoiThoai bắt đầu dialogue (player nhấn Space để xem)
///   3. Khi dialogue kết thúc → Gia Nô chạy về, player được mở khóa
/// </summary>
public class GiaNoCutscene : MonoBehaviour
{
    [Header("Waypoints")]
    [Tooltip("Vị trí Gia Nô đứng để báo tin")]
    public Transform diemBaoTin;

    [Tooltip("Vị trí cửa nhà (điểm xuất phát và trở về)")]
    public Transform diemCuaNha;

    [Tooltip("Vị trí cạnh giường (đích cuối sau khi báo tin)")]
    public Transform diemCanhGiuong;

    [Header("Config")]
    public float tocDo = 4f;

    [Header("References")]
    [Tooltip("PlayerMovement_Cutscene của TranQuocTuan")]
    public PlayerMovement_Cutscene playerScript;

    [Tooltip("QuanLyHoiThoai dùng để chạy dialogue Gia Nô báo tin")]
    public QuanLyHoiThoai heThongThoai;

    [Tooltip("Animator của GiaNo (kéo GameObject GiaNo vào đây)")]
    public Animator giaNhAnim;

    private Animator anim;

    void Start()
    {
        anim = giaNhAnim != null ? giaNhAnim : GetComponent<Animator>();

        if (playerScript == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                playerScript = playerObj.GetComponent<PlayerMovement_Cutscene>();
        }

        if (heThongThoai == null)
            heThongThoai = FindObjectOfType<QuanLyHoiThoai>();

        if (diemCuaNha != null)
        {
            Transform actorTransform = giaNhAnim != null ? giaNhAnim.transform : transform;
            actorTransform.position = diemCuaNha.position;
        }

        StartCoroutine(Pha1_GiaNoBaoTin());
    }

    // ─── Pha 1: Gia Nô chạy ra, dialogue bắt đầu ────────────────────────

    IEnumerator Pha1_GiaNoBaoTin()
    {
        KhoaPlayer(huongNhin: Vector2.right);

        anim.Play("GiaNo_WalkDown");
        yield return ChayDen(diemBaoTin, tocDo * 2f);

        anim.Play("GiaNo_IdleDown");

        QuayPlayer(huongNhin: Vector2.left);

        // Bật dialogue — player nhấn Space để xem, không hardcode thời gian
        if (heThongThoai != null)
        {
            heThongThoai.tenSceneTiepTheo = ""; // không chuyển scene tự động
            heThongThoai.OnEnd = () => StartCoroutine(Pha2_GiaNoChatLui());
            heThongThoai.BatDauThoai();
        }
        else
        {
            Debug.LogWarning("[GiaNoCutscene] Chưa gán heThongThoai — bỏ qua dialogue.");
            StartCoroutine(Pha2_GiaNoChatLui());
        }
    }

    // ─── Pha 2: Sau dialogue → Gia Nô rút, mở khóa player ──────────────

    IEnumerator Pha2_GiaNoChatLui()
    {
        MoKhoaPlayer();

        anim.Play("GiaNo_WalkUp");
        yield return ChayDen(diemCuaNha, tocDo * 2f);

        Transform actorTransform = giaNhAnim != null ? giaNhAnim.transform : transform;
        actorTransform.position = diemCanhGiuong.position;
        anim.Play("GiaNo_IdleDown");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    void KhoaPlayer(Vector2 huongNhin)
    {
        if (playerScript == null) return;
        playerScript.canMove = false;
        var rb = playerScript.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        QuayPlayer(huongNhin);
    }

    void QuayPlayer(Vector2 huongNhin)
    {
        if (playerScript == null) return;
        var anim = playerScript.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetFloat("MoveX", huongNhin.x);
            anim.SetFloat("MoveY", huongNhin.y);
            anim.speed = 0f;
        }
    }

    void MoKhoaPlayer()
    {
        if (playerScript != null) playerScript.canMove = true;
    }

    IEnumerator ChayDen(Transform dich, float tocDoChay)
    {
        Transform actorTransform = giaNhAnim != null ? giaNhAnim.transform : transform;
        var rb = actorTransform.GetComponent<Rigidbody2D>();
        
        while (Vector2.Distance(actorTransform.position, dich.position) > 0.1f)
        {
            Vector2 newPos = Vector2.MoveTowards(actorTransform.position, dich.position, tocDoChay * Time.fixedDeltaTime);
            if (rb != null)
            {
                rb.MovePosition(newPos);
            }
            else
            {
                actorTransform.position = new Vector3(newPos.x, newPos.y, actorTransform.position.z);
            }
            yield return new WaitForFixedUpdate();
        }
    }
}
