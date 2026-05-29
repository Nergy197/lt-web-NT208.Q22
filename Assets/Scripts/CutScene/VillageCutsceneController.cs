using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Cutscene làng hoang tàn.
/// Mọi thứ (scale background, scale nhân vật, vị trí) chỉnh tay trong Editor.
/// Script chỉ lo: kích hoạt khi player vào trigger + cuộn camera từ phải sang trái.
///
/// Setup:
///   1. Chỉnh VillageBackground scale/position cho vừa ý trong Scene view
///   2. Chỉnh nhân vật scale/position cho vừa ý
///   3. Chỉnh Main Camera Orthographic Size cho vừa background
///   4. Kéo các field vào Inspector
///   5. Đặt CameraStartX = X bên phải background, CameraEndX = X bên trái
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VillageCutsceneController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    [Tooltip("cm vcam1 — tắt khi cutscene chạy")]
    public GameObject virtualCamera;
    public Animator characterAnimator;
    public Transform character;

    [Header("UI cần ẩn")]
    public GameObject[] uiToHide;

    [Header("Camera Scroll — chỉnh tay")]
    [Tooltip("X bắt đầu (bên phải background) — chỉnh trong Scene view")]
    public float cameraStartX = 10f;
    [Tooltip("X kết thúc (bên trái background) — chỉnh trong Scene view")]
    public float cameraEndX = -10f;
    [Tooltip("Y cố định của camera trong cutscene")]
    public float cameraY = 0f;
    [Tooltip("Tốc độ cuộn sang trái")]
    public float scrollSpeed = 2f;

    [Header("Scene tiếp theo")]
    public string nextScene = "";

    [Header("Dialogue (tuỳ chọn)")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    [TextArea(2, 4)]
    public string[] dialogueLines;
    [Tooltip("Giây xuất hiện từng dòng thoại")]
    public float[] dialogueTimes;

    bool _triggered = false;
    int _dialogueIndex = 0;
    float _elapsed = 0f;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        _triggered = true;
        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        // Tắt Cinemachine
        if (virtualCamera != null) virtualCamera.SetActive(false);
        foreach (var comp in cam.GetComponents<Behaviour>())
            if (comp.GetType().Name == "CinemachineBrain")
                comp.enabled = false;

        // Ẩn UI
        foreach (var ui in uiToHide)
            if (ui != null) ui.SetActive(false);

        // Đặt camera vào vị trí bắt đầu
        cam.transform.position = new Vector3(cameraStartX, cameraY, cam.transform.position.z);

        // Bật animation đi
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat("MoveX", -1f);
            characterAnimator.SetFloat("MoveY", 0f);
            characterAnimator.speed = 1f;
        }

        _elapsed = 0f;
        _dialogueIndex = 0;

        while (cam.transform.position.x > cameraEndX)
        {
            Vector3 pos = cam.transform.position;
            pos.x = Mathf.Max(pos.x - scrollSpeed * Time.deltaTime, cameraEndX);
            cam.transform.position = pos;

            // Nhân vật đi theo camera (giữ nguyên Y và scale đã set sẵn)
            if (character != null)
            {
                Vector3 cp = character.position;
                cp.x = cam.transform.position.x - 2f;
                character.position = cp;
            }

            _elapsed += Time.deltaTime;
            TryShowDialogue();
            yield return null;
        }

        if (characterAnimator != null) characterAnimator.speed = 0f;
        yield return new WaitForSeconds(1.5f);

        if (!string.IsNullOrEmpty(nextScene))
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    void TryShowDialogue()
    {
        if (dialogueLines == null || dialogueTimes == null) return;
        if (_dialogueIndex >= dialogueLines.Length || _dialogueIndex >= dialogueTimes.Length) return;
        if (_elapsed >= dialogueTimes[_dialogueIndex])
        {
            ShowDialogue(dialogueLines[_dialogueIndex++]);
        }
    }

    void ShowDialogue(string line)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueText != null) dialogueText.text = line;
        StopCoroutine(nameof(HideDialogue));
        StartCoroutine(nameof(HideDialogue));
    }

    IEnumerator HideDialogue()
    {
        yield return new WaitForSeconds(4f);
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }
}
