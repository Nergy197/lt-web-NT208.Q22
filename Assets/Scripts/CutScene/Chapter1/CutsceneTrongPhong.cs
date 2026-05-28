using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneTrongPhong : MonoBehaviour
{
    [Header("Cấu Hình")]
    public PlayerMovement_Cutscene playerScript;
    public Transform diemCanhGiuong;
    public float khoangCachTuongTac = 2.0f;
    public GameObject khungThoaiUI;

    [Header("Chuyển Scene sau khi xong")]
    public string nextScene = "MapScene";
    public float delayBeforeLoad = 1f;

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

        // Chờ GiaNoCutscene giải phóng player trước (tránh ăn flag interact của dialogue đầu)
        if (playerScript != null && !playerScript.canMove) return;

        // Kiểm tra tương tác (mobile button hoặc phím F)
        if (playerScript != null && diemCanhGiuong != null)
        {
            float khoangCach = Vector2.Distance(playerScript.transform.position, diemCanhGiuong.position);
            MobileInteractRegistry.SetActive(this, khoangCach <= khoangCachTuongTac);

            if (khoangCach <= khoangCachTuongTac && IsInteractPressed())
            {
                Debug.Log("<color=green>Đã bấm tương tác thành công!</color>");
                KichHoatHoiThoai();
            }
        }
    }

    bool IsInteractPressed()
    {
        if (InputController.Instance != null)
            return InputController.Instance.IsInteractPressed();

        // Fallback khi scene test thiếu InputController.
        return Input.GetKeyDown(KeyCode.F);
    }

    void KichHoatHoiThoai()
    {
        dangTrongCuocThoai = true;
        MobileInteractRegistry.SetActive(this, false);
        if (playerScript != null)
        {
            playerScript.canMove = false;
            Animator anim = playerScript.GetComponent<Animator>();
            if (anim != null) anim.speed = 0f;
        }

        if (khungThoaiUI != null) khungThoaiUI.SetActive(true);
        if (heThongThoai != null) heThongThoai.BatDauThoai();
    }

    void OnDisable()
    {
        MobileInteractRegistry.SetActive(this, false);
    }

    void KetThucHoiThoai()
    {
        dangTrongCuocThoai = false;
        if (khungThoaiUI != null) khungThoaiUI.SetActive(false);
        if (playerScript != null) playerScript.canMove = true;

        Debug.Log("[CutsceneTrongPhong] Hội thoại phòng ngủ kết thúc.");

        if (!string.IsNullOrEmpty(nextScene))
        {
            Debug.Log($"[CutsceneTrongPhong] Chờ {delayBeforeLoad}s → load '{nextScene}'");
            StartCoroutine(LoadSceneDelayed());
        }
    }

    IEnumerator LoadSceneDelayed()
    {
        // Lưu game trước khi chuyển sang MapScene
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuickSaveToLocal();
            Debug.Log("[CutsceneTrongPhong] Đã lưu game sau cutscene.");
        }

        yield return new WaitForSeconds(delayBeforeLoad);
        SceneManager.LoadScene(nextScene);
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