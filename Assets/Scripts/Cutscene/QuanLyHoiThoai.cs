using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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
    public CauThoai[] kichBan;
    public float tocDoGo = 0.05f;

    [Header("Chuyển Scene")]
    [Tooltip("Tên Scene tiếp theo. Rỗng = không chuyển.")]
    public string tenSceneTiepTheo = "MapScene";

    [Header("Quest Actions")]
    [Tooltip("Các hành động quest kích hoạt khi dialogue kết thúc.\n" +
             "Kéo QuestSO vào Quest, chọn TriggerOn = OnDialogueEnd.")]
    public List<QuestAction> questActions = new();

    // ─── Runtime ─────────────────────────────────────────────────────────

    private int  cauHienTai       = 0;
    private bool dangGoChu        = false;
    private bool daXongCauHienTai = false;
    private bool daKetThuc        = false;
    private bool dangHoatDong     = false; // true khi dialogue đang chạy
    private Coroutine goChuCoroutine;

    // ─── API ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Ẩn tất cả bóng thoại ngay lúc bắt đầu Scene để tránh bị lọt màn hình
        if (kichBan != null)
        {
            foreach (var cau in kichBan)
                if (cau.bongBongUI != null) cau.bongBongUI.SetActive(false);
        }
    }

    public void BatDauThoai()
    {
        if (kichBan == null || kichBan.Length == 0)
        {
            Debug.LogWarning("[DIALOGUE] kichBan rỗng — chưa nhập câu thoại trong Inspector.");
            OnEnd?.Invoke();
            return;
        }

        foreach (var cau in kichBan)
            if (cau.bongBongUI != null) cau.bongBongUI.SetActive(false);

        cauHienTai    = 0;
        daKetThuc     = false;
        dangHoatDong  = true;

        DangKyInput();
        HienThiCauTiepTheo();
    }

    // ─── Input ───────────────────────────────────────────────────────────

    void DangKyInput()
    {
        var ic = InputController.Instance;
        if (ic != null)
        {
            ic.SetMode(InputMode.Cutscene);
            ic.Input.Map.Interact.performed += OnAdvance;
        }
    }

    void HuyDangKyInput()
    {
        var ic = InputController.Instance;
        if (ic != null)
            ic.Input.Map.Interact.performed -= OnAdvance;
    }

    void OnAdvance(InputAction.CallbackContext ctx) => XuLyAdvance();

    // Fallback khi không có InputController (test trực tiếp scene)
    void Update()
    {
        if (!dangHoatDong || daKetThuc) return;
        if (InputController.Instance != null) return; // InputController đang xử lý

        if (UnityEngine.Input.GetKeyDown(KeyCode.Space)
            || UnityEngine.Input.GetKeyDown(KeyCode.Return)
            || UnityEngine.Input.GetMouseButtonDown(0))
            XuLyAdvance();
    }

    void XuLyAdvance()
    {
        if (daKetThuc) return;

        if (daXongCauHienTai)
        {
            kichBan[cauHienTai].bongBongUI.SetActive(false);
            cauHienTai++;

            if (cauHienTai < kichBan.Length)
                HienThiCauTiepTheo();
            else
                KetThucThoai();
        }
        else if (dangGoChu)
        {
            if (goChuCoroutine != null)
            {
                StopCoroutine(goChuCoroutine);
                goChuCoroutine = null;
            }
            kichBan[cauHienTai].txtNoiDung.text = kichBan[cauHienTai].noiDungThoai;
            dangGoChu        = false;
            daXongCauHienTai = true;
        }
    }

    void OnDisable() => HuyDangKyInput();

    // ─── Internal ────────────────────────────────────────────────────────

    void HienThiCauTiepTheo()
    {
        var cau = kichBan[cauHienTai];
        if (cau.bongBongUI == null)
        {
            Debug.LogError($"[DIALOGUE] kichBan[{cauHienTai}].bongBongUI chưa được gán trong Inspector.");
            return;
        }
        if (cau.txtNoiDung == null)
        {
            Debug.LogError($"[DIALOGUE] kichBan[{cauHienTai}].txtNoiDung chưa được gán trong Inspector.");
            return;
        }
        cau.bongBongUI.SetActive(true);
        goChuCoroutine = StartCoroutine(GoChu(cau));
    }

    IEnumerator GoChu(CauThoai cau)
    {
        dangGoChu        = true;
        daXongCauHienTai = false;
        cau.txtNoiDung.text = "";

        foreach (char c in cau.noiDungThoai.ToCharArray())
        {
            cau.txtNoiDung.text += c;
            yield return new WaitForSeconds(tocDoGo);
        }

        dangGoChu        = false;
        daXongCauHienTai = true;
    }

    /// <summary>Callback khi dialogue kết thúc (trước khi chuyển scene).</summary>
    public System.Action OnEnd;

    void KetThucThoai()
    {
        daKetThuc    = true;
        dangHoatDong = false;
        HuyDangKyInput();

        var ic = InputController.Instance;
        if (ic != null) ic.SetMode(InputMode.Map);

        Debug.Log("[DIALOGUE] Đã diễn xong kịch bản.");

        QuestAction.Execute(questActions, QuestAction.When.OnDialogueEnd);
        
        var tempEnd = OnEnd;
        OnEnd = null;
        tempEnd?.Invoke();

        if (!string.IsNullOrEmpty(tenSceneTiepTheo))
            UnityEngine.SceneManagement.SceneManager.LoadScene(tenSceneTiepTheo);
    }
}
