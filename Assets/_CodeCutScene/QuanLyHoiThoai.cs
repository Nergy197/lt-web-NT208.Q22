using UnityEngine;
using UnityEngine.UI; // Dùng thư viện UI để điều khiển chữ
using System.Collections;

// Cấu trúc của 1 câu thoại
[System.Serializable]
public class CauThoai
{
    [Tooltip("Bong bóng thoại của người đang nói (Kéo BongBong_Cha hoặc BongBong_Con vào)")]
    public GameObject bongBongUI;

    [Tooltip("Cái component Text nằm bên trong bong bóng đó")]
    public TMPro.TextMeshProUGUI txtNoiDung;

    [TextArea(2, 5)]
    [Tooltip("Nhập nội dung câu nói vào đây")]
    public string noiDungThoai;
}

public class QuanLyHoiThoai : MonoBehaviour
{
    public CauThoai[] kichBan; // Danh sách các câu thoại
    public float tocDoGo = 0.05f;

    private int cauHienTai = 0;
    private bool dangGoChu = false;
    private bool daXongCauHienTai = false;

    public void BatDauThoai()
    {
        // Tắt hết các bong bóng trước khi diễn
        foreach (var cau in kichBan)
        {
            if (cau.bongBongUI != null) cau.bongBongUI.SetActive(false);
        }

        cauHienTai = 0;
        HienThiCauTiepTheo();
    }

    void Update()
    {
        // Bấm phím Space hoặc Click chuột trái để tương tác
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            // 1. Nếu chữ đã chạy xong -> Chuyển sang câu tiếp theo
            if (daXongCauHienTai)
            {
                kichBan[cauHienTai].bongBongUI.SetActive(false); // Tắt bong bóng cũ
                cauHienTai++; // Tăng số thứ tự câu lên

                if (cauHienTai < kichBan.Length)
                {
                    HienThiCauTiepTheo(); // Hiện câu tiếp
                }
                else
                {
                    // HẾT KỊCH BẢN
                    KetThucThoai();
                }
            }
            // 2. Nếu chữ đang chạy rề rề mà người chơi bấm phím -> Hiện full câu luôn cho lẹ
            else if (dangGoChu)
            {
                StopAllCoroutines();
                kichBan[cauHienTai].txtNoiDung.text = kichBan[cauHienTai].noiDungThoai;
                dangGoChu = false;
                daXongCauHienTai = true;
            }
        }
    }

    void HienThiCauTiepTheo()
    {
        var cau = kichBan[cauHienTai];
        cau.bongBongUI.SetActive(true); // Bật bong bóng của người nói lượt này lên
        StartCoroutine(GoChu(cau));
    }

    IEnumerator GoChu(CauThoai cau)
    {
        dangGoChu = true;
        daXongCauHienTai = false;
        cau.txtNoiDung.text = ""; // Xóa trắng chữ cũ

        // Chạy từng chữ cái một
        foreach (char c in cau.noiDungThoai.ToCharArray())
        {
            cau.txtNoiDung.text += c;
            yield return new WaitForSeconds(tocDoGo);
        }

        dangGoChu = false;
        daXongCauHienTai = true;
    }

    void KetThucThoai()
    {
        Debug.Log("Đã diễn xong kịch bản!");
        // Ở đây lát nữa mình sẽ gọi lệnh làm tối màn hình (Fade to Black)
    }
}