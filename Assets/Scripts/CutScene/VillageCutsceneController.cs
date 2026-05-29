using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Cutscene làng hoang tàn.
/// Tự động nhân bản background thành N bản nối tiếp nhau,
/// camera cuộn liên tục từ phải sang trái không giật.
///
/// Setup:
///   1. Chỉnh VillageBackground scale/position cho vừa ý
///   2. Chỉnh nhân vật scale/position cho vừa ý
///   3. Chỉnh Main Camera Orthographic Size cho vừa chiều cao background
///   4. Kéo các field vào Inspector
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VillageCutsceneController : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public SpriteRenderer background;
    public Animator characterAnimator;
    public Transform character;

    [Header("Scroll Settings")]
    [Tooltip("Số lần lặp background (4 = 4 bản nối tiếp nhau)")]
    public int loopCount = 4;
    [Tooltip("Tốc độ cuộn sang trái")]
    public float scrollSpeed = 2f;
    [Tooltip("Y cố định của camera")]
    public float cameraY = 0f;
    [Tooltip("Offset X nhân vật so với tâm camera")]
    public float characterOffsetX = 7f;

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
        // Tắt PlayerMovement
        if (character != null)
            foreach (var comp in character.GetComponents<MonoBehaviour>())
                if (comp.GetType().Name == "PlayerMovement") comp.enabled = false;

        // Tính chiều rộng 1 bản background
        float bgWidth = background.bounds.size.x;

        // Nhân bản background nối tiếp nhau
        // Bản gốc ở vị trí index 0, các bản copy ở bên phải
        float originX = background.transform.position.x;
        float originY = background.transform.position.y;

        for (int i = 1; i < loopCount; i++)
        {
            SpriteRenderer copy = Instantiate(background, transform);
            copy.transform.position = new Vector3(originX + bgWidth * i, originY, background.transform.position.z);
        }

        // Camera bắt đầu ở cạnh phải của toàn bộ dải background
        float totalWidth = bgWidth * loopCount;
        float camHalfW = cam.orthographicSize * cam.aspect;
        float startX = originX + totalWidth / 2f - camHalfW;
        float endX   = originX - totalWidth / 2f + camHalfW;

        cam.transform.position = new Vector3(startX, cameraY != 0 ? cameraY : originY, cam.transform.position.z);

        // Animation đi trái
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat("MoveX", -1f);
            characterAnimator.SetFloat("MoveY", 0f);
            characterAnimator.speed = 1f;
        }

        _elapsed = 0f;
        _dialogueIndex = 0;

        // Cuộn liên tục, không giật
        while (cam.transform.position.x > endX)
        {
            Vector3 pos = cam.transform.position;
            pos.x = Mathf.Max(pos.x - scrollSpeed * Time.deltaTime, endX);
            cam.transform.position = pos;

            if (character != null)
            {
                Vector3 cp = character.position;
                cp.x = cam.transform.position.x + characterOffsetX;
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
            ShowDialogue(dialogueLines[_dialogueIndex++]);
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
