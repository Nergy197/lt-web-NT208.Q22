using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class DieuKhienCutscene : MonoBehaviour
{
    [Header("Kết nối UI")]
    public Image khungAnhUI;
    public TextMeshProUGUI khungChuUI;
    public TextMeshProUGUI textChuyenCanhUI;

    private CanvasGroup cgAnh;
    private CanvasGroup cgChu;
    private CanvasGroup cgChuyenCanh;

    [Header("Cài đặt hiệu ứng")]
    public float tocDoFadeAnh = 1.5f; // Tốc độ hiện/ẩn của ảnh
    public float tocDoFadeChu = 1.0f; // Tốc độ hiện/ẩn của chữ (thoại và chữ chuyển cảnh)
    public float thoiGianNghi = 3f;
    public string tenSceneTiepTheo = "TutorialScene";

    public Sprite[] danhSachAnh;
    private string[] danhSachCauThoai;

    void Start()
    {
        // Kiểm tra và lấy CanvasGroup để tránh lỗi
        cgAnh = LayHoacThemCanvasGroup(khungAnhUI.gameObject);
        cgChu = LayHoacThemCanvasGroup(khungChuUI.gameObject);
        cgChuyenCanh = LayHoacThemCanvasGroup(textChuyenCanhUI.gameObject);

        // Ẩn tất cả lúc bắt đầu
        cgAnh.alpha = 0;
        cgChu.alpha = 0;
        cgChuyenCanh.alpha = 0;

        NapCauThoai();
        StartCoroutine(ChayKichBanSieuPham());
    }

    // Hàm phụ trợ để tự động thêm CanvasGroup nếu anh quên
    CanvasGroup LayHoacThemCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    IEnumerator ChayKichBanSieuPham()
    {
        // VÒNG LẶP CHO 8 HÌNH ẢNH ĐẦU TIÊN
        for (int i = 0; i < danhSachAnh.Length; i++)
        {
            // Ẩn ảnh và chữ cũ (Dùng tốc độ riêng biệt)
            yield return StartCoroutine(Fade(cgAnh, 0, tocDoFadeAnh));
            yield return StartCoroutine(Fade(cgChu, 0, tocDoFadeChu));

            khungAnhUI.sprite = danhSachAnh[i];
            khungChuUI.text = danhSachCauThoai[i];

            // Hiện ảnh mới (Dùng tốc độ ảnh)
            yield return StartCoroutine(Fade(cgAnh, 1, tocDoFadeAnh));

            yield return new WaitForSeconds(0.5f);

            // Hiện chữ mới (Dùng tốc độ chữ)
            yield return StartCoroutine(Fade(cgChu, 1, tocDoFadeChu));

            yield return new WaitForSeconds(thoiGianNghi);
        }

        // --- CHUYỂN CẢNH SANG TRẦN LIỄU ---

        // Làm mờ ảnh và chữ lề
        yield return StartCoroutine(Fade(cgAnh, 0, tocDoFadeAnh));
        yield return StartCoroutine(Fade(cgChu, 0, tocDoFadeChu));

        yield return new WaitForSeconds(1f);

        // Hiện dòng chữ lớn chính giữa (Dùng tốc độ chữ)
        yield return StartCoroutine(Fade(cgChuyenCanh, 1, tocDoFadeChu));

        yield return new WaitForSeconds(thoiGianNghi + 1f);

        // Mờ dần dòng chữ chính giữa
        yield return StartCoroutine(Fade(cgChuyenCanh, 0, tocDoFadeChu));
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(tenSceneTiepTheo);
    }

    IEnumerator Fade(CanvasGroup cg, float targetAlpha, float speed)
    {
        while (!Mathf.Approximately(cg.alpha, targetAlpha))
        {
            cg.alpha = Mathf.MoveTowards(cg.alpha, targetAlpha, Time.deltaTime * speed);
            yield return null;
        }
        cg.alpha = targetAlpha;
    }

    void NapCauThoai()
    {
        danhSachCauThoai = new string[8];
        danhSachCauThoai[0] = "Thuở xa xưa, một mầm mống từ cõi hư không xé toạc bầu trời, giáng xuống thảo nguyên cằn cỗi...";
        danhSachCauThoai[1] = "Nó cắm sâu bộ rễ dơ bẩn vào lòng đất, hút lấy tinh hoa và sự oán hận của thế gian.";
        danhSachCauThoai[2] = "Năm tháng qua đi, mầm quỷ trưởng thành. Một loài quả mang sức mạnh cấm kỵ đơm bông.";
        danhSachCauThoai[3] = "Kẻ khao khát quyền lực đã tìm đến...";
        danhSachCauThoai[4] = "...và không ngần ngại nuốt trọn tai ương.";
        danhSachCauThoai[5] = "Nhục thể vỡ nát, ác quỷ giáng thế. Sức mạnh vô song, nhưng tâm trí vẫn sắc lạnh như gươm giáo.";
        danhSachCauThoai[6] = "Được dẫn dắt bởi sức mạnh phi phàm, đạo quân quỷ dữ tràn đi gieo rắc kinh hoàng, nghiền nát mọi sự kháng cự.";
        danhSachCauThoai[7] = "Hắc ám nhuộm đen toàn lục địa... Đó là lúc thế giới phải cúi đầu rạp mình trước móng vuốt của Đại Đế Quốc Mông Cổ.";
    }
}