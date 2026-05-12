using UnityEngine;
using System.Collections;

public class GiaNoCutscene : MonoBehaviour
{
    [Header("Cấu Hình Nhân Vật")]
    public Transform giaNoBody;
    public Animator giaNoAnim;

    [Header("Cấu Hình Hội Thoại")]
    public QuanLyHoiThoai scriptHoiThoai;
    public GameObject khungThoaiUI;

    [Header("Điểm Di Chuyển")]
    public Transform diemBaoTin;
    public Transform diemCuaNha;
    public Transform diemCanhGiuong;
    public float tocDo = 4f;

    [Header("Người Chơi")]
    public PlayerMovement_Cutscene playerScript;

    void Start()
    {
        if (giaNoBody == null || giaNoAnim == null || scriptHoiThoai == null)
        {
            Debug.LogError("Anh Chuẩn ơi! Nhớ kéo đủ đồ vào Manager nha!");
            return;
        }

        giaNoBody.position = diemCuaNha.position;
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);

        StartCoroutine(DienCangBaoTin());
    }

    IEnumerator DienCangBaoTin()
    {
        // 1. Khóa người chơi và cho quay mặt sang PHẢI (MoveX = 1)
        if (playerScript != null)
        {
            playerScript.canMove = false;
            Animator playerAnim = playerScript.GetComponent<Animator>();
            if (playerAnim != null)
            {
                playerAnim.SetFloat("MoveX", 1f);
                playerAnim.SetFloat("MoveY", 0f);
            }
        }

        // 2. Gia nô chạy ra
        giaNoAnim.Play("GiaNo_WalkDown");
        while (Vector3.Distance(giaNoBody.position, diemBaoTin.position) > 0.1f)
        {
            giaNoBody.position = Vector3.MoveTowards(giaNoBody.position, diemBaoTin.position, (tocDo * 2f) * Time.deltaTime);
            yield return null;
        }

        // 3. Tới nơi & Đứng im (ĐÃ BỎ ĐOẠN QUAY TRÁI TẠI ĐÂY)
        giaNoAnim.Play("GiaNo_IdleDown");

        // Không còn dòng: playerAnim.SetFloat("MoveX", -1f); 
        // Nhân vật sẽ giữ nguyên hướng nhìn bên phải từ bước 1

        // 4. BẮT ĐẦU NÓI
        if (khungThoaiUI != null) khungThoaiUI.SetActive(true);
        scriptHoiThoai.BatDauThoai();

        while (scriptHoiThoai.enabled && !scriptHoiThoai.daXongHetKichBan)
        {
            yield return null;
        }

        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);
        if (playerScript != null) playerScript.canMove = true;

        // 5. Chạy vào nhà
        giaNoAnim.Play("GiaNo_WalkUp");
        while (Vector3.Distance(giaNoBody.position, diemCuaNha.position) > 0.1f)
        {
            giaNoBody.position = Vector3.MoveTowards(giaNoBody.position, diemCuaNha.position, (tocDo * 2f) * Time.deltaTime);
            yield return null;
        }

        // 6. ĐỨNG CẠNH GIƯỜNG
        giaNoBody.position = diemCanhGiuong.position;
        giaNoAnim.Play("GiaNo_IdleDown");
    }
}