using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class CauThoai
{
    public GameObject bongBongUI;
    public TMPro.TextMeshProUGUI txtNoiDung;
    [TextArea(2, 5)]
    public string noiDungThoai;
}

public class QuanLyHoiThoai : MonoBehaviour
{
    public CauThoai[] kichBan;
    public float tocDoGo = 0.05f;

    private int cauHienTai = 0;
    private bool dangGoChu = false;
    private bool daXongCauHienTai = false;

    [HideInInspector] public bool daXongHetKichBan = false;

    public void BatDauThoai()
    {
        daXongHetKichBan = false;
        cauHienTai = 0;

        foreach (var cau in kichBan)
        {
            if (cau.bongBongUI != null) cau.bongBongUI.SetActive(false);
        }

        HienThiCauTiepTheo();
    }

    void Update()
    {
        // TẤM KHIÊN 1: Nếu đã xong hết kịch bản thì nghỉ, không nhận phím nữa
        if (daXongHetKichBan) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (daXongCauHienTai)
            {
                // TẤM KHIÊN 2: Kiểm tra xem chỉ số có nằm trong mảng không trước khi tắt UI
                if (cauHienTai >= 0 && cauHienTai < kichBan.Length)
                {
                    if (kichBan[cauHienTai].bongBongUI != null)
                        kichBan[cauHienTai].bongBongUI.SetActive(false);
                }

                cauHienTai++;

                // Kiểm tra xem còn câu tiếp theo không
                if (cauHienTai < kichBan.Length)
                {
                    HienThiCauTiepTheo();
                }
                else
                {
                    daXongHetKichBan = true; // Đã hết kịch bản thật sự
                    KetThucThoai();
                }
            }
            else if (dangGoChu)
            {
                StopAllCoroutines();
                if (cauHienTai < kichBan.Length)
                    kichBan[cauHienTai].txtNoiDung.text = kichBan[cauHienTai].noiDungThoai;

                dangGoChu = false;
                daXongCauHienTai = true;
            }
        }
    }

    void HienThiCauTiepTheo()
    {
        if (cauHienTai < kichBan.Length)
        {
            var cau = kichBan[cauHienTai];
            if (cau.bongBongUI != null) cau.bongBongUI.SetActive(true);
            StartCoroutine(GoChu(cau));
        }
    }

    IEnumerator GoChu(CauThoai cau)
    {
        dangGoChu = true;
        daXongCauHienTai = false;
        cau.txtNoiDung.text = "";

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
    }
}