using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(QuanLyHoiThoai))]
public class AutoStartCutscene : MonoBehaviour
{
    [Tooltip("Điền tên Scene tiếp theo vào đây (vd: Chapter1_Village)")]
    public string nextSceneName; 

    [Header("Hiệu ứng mở đầu (Intro)")]
    [Tooltip("Kéo IntroPanel (mảng đen) vào đây. Nhớ thêm component CanvasGroup cho nó.")]
    public CanvasGroup introPanel;
    public float introDuration = 2f;
    public float fadeOutDuration = 1f;

    [Header("Hiệu ứng kết thúc (Outro)")]
    [Tooltip("Kéo OutroPanel (mảng đen) vào đây để làm màn hình chuyển cảnh.")]
    public CanvasGroup outroPanel;
    public float outroDuration = 2f;
    public float fadeInDuration = 1f;
    
    private QuanLyHoiThoai thoai;
    private bool daChuyenScene = false;

    void Start()
    {
        thoai = GetComponent<QuanLyHoiThoai>();
        
        // Cực kỳ quan trọng: Khóa trạng thái lại để ngăn Update chuyển cảnh sớm khi đang chạy Intro
        thoai.daXongHetKichBan = false;
        
        if (introPanel != null)
        {
            StartCoroutine(PlayIntroSequence());
        }
        else
        {
            thoai.BatDauThoai();
        }
    }

    IEnumerator PlayIntroSequence()
    {
        // Đảm bảo Intro hiển thị rõ lúc đầu
        introPanel.alpha = 1f;
        introPanel.gameObject.SetActive(true);
        
        // Chờ vài giây cho người chơi đọc chữ
        yield return new WaitForSeconds(introDuration);

        // Mờ dần màn hình đen
        float timer = 0;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            introPanel.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
            yield return null;
        }

        // Tắt hẳn và bắt đầu thoại
        introPanel.gameObject.SetActive(false);
        thoai.BatDauThoai();
    }

    void Update()
    {
        // Khi thoại kết thúc và chưa chuyển scene
        if (thoai.daXongHetKichBan && !daChuyenScene)
        {
            daChuyenScene = true;
            if (outroPanel != null)
            {
                StartCoroutine(PlayOutroSequence());
            }
            else
            {
                LoadNextScene();
            }
        }
    }

    IEnumerator PlayOutroSequence()
    {
        outroPanel.gameObject.SetActive(true);
        outroPanel.alpha = 0f;
        
        // Mờ dần thành màn hình đen
        float timer = 0;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            outroPanel.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
            yield return null;
        }

        // Hiện chữ "Tại chiến trường..."
        yield return new WaitForSeconds(outroDuration);

        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
