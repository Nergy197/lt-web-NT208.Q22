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

    [HideInInspector] public bool daXongHetKichBan = true;

    public void BatDauThoai()
    {
        daXongHetKichBan = false;
        cauHienTai = 0;
        MobileInteractRegistry.SetActive(this, true);

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

        if (IsAdvancePressed())
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
                {
                    kichBan[cauHienTai].txtNoiDung.text = kichBan[cauHienTai].noiDungThoai;
                    kichBan[cauHienTai].txtNoiDung.maxVisibleCharacters = 99999;
                }

                dangGoChu = false;
                daXongCauHienTai = true;
            }
        }
    }

    bool IsAdvancePressed()
    {
        bool pressed = false;
        if (InputController.Instance != null)
        {
            pressed = InputController.Instance.IsInteractPressed();
        }

        // Luôn cho phép bấm Space, click chuột (hoặc chạm màn hình) để qua thoại
        return pressed || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
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
        
        cau.txtNoiDung.text = cau.noiDungThoai;
        cau.txtNoiDung.ForceMeshUpdate();
        int totalChars = cau.txtNoiDung.textInfo.characterCount;

        for (int i = 0; i <= totalChars; i++)
        {
            cau.txtNoiDung.maxVisibleCharacters = i;
            yield return new WaitForSeconds(tocDoGo);
        }

        dangGoChu = false;
        daXongCauHienTai = true;
    }

    void KetThucThoai()
    {
        MobileInteractRegistry.SetActive(this, false);
        Debug.Log("Đã diễn xong kịch bản!");
    }

    void OnDisable()
    {
        MobileInteractRegistry.SetActive(this, false);
    }
}