using UnityEngine;
using System.Collections;

public class CutsceneTrongPhong : MonoBehaviour
{
    [Header("Cấu Hình")]
    public PlayerMovement_Cutscene playerScript;
    public Transform diemCanhGiuong;
    public float khoangCachTuongTac = 2.0f; // Khoảng cách để bấm F
    public GameObject khungThoaiUI;

    private bool dangTrongCuocThoai = false;
    private QuanLyHoiThoai heThongThoai;

    void Start()
    {
        heThongThoai = GetComponent<QuanLyHoiThoai>();
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);
    }

    void Update()
    {
        // Nếu đang nói chuyện thì không cho bấm F nữa
        if (dangTrongCuocThoai)
        {
            if (heThongThoai != null && heThongThoai.daXongHetKichBan)
            {
                KetThucHoiThoai();
            }
            return;
        }

        // Kiểm tra bấm phím F
        if (playerScript != null && diemCanhGiuong != null)
        {
            float khoangCach = Vector2.Distance(playerScript.transform.position, diemCanhGiuong.position);

            if (khoangCach <= khoangCachTuongTac && Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("<color=green>Đã bấm F thành công!</color>");
                KichHoatHoiThoai();
            }
        }
    }

    void KichHoatHoiThoai()
    {
        dangTrongCuocThoai = true;
        if (playerScript != null)
        {
            playerScript.canMove = false;
            Animator anim = playerScript.GetComponent<Animator>();
            if (anim != null) anim.speed = 0f;
        }

        if (khungThoaiUI != null) khungThoaiUI.SetActive(true);
        if (heThongThoai != null) heThongThoai.BatDauThoai();
    }

    void KetThucHoiThoai()
    {
        dangTrongCuocThoai = false;
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);
        if (playerScript != null) playerScript.canMove = true;
    }

    // Vẽ vòng tròn để anh dễ thấy vùng bấm F trong Scene
    void OnDrawGizmosSelected()
    {
        if (diemCanhGiuong != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(diemCanhGiuong.position, khoangCachTuongTac);
        }
    }
}