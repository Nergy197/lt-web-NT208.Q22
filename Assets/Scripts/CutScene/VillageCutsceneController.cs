using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Cutscene làng hoang tàn.
/// Scroll chạy song song với dialogue — mỗi dòng thoại: hiện → đợi → ẩn → đợi gap.
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
    [Tooltip("Số lần lặp background")]
    public int loopCount = 4;
    public float scrollSpeed = 2f;
    [Tooltip("Y cố định của camera (0 = dùng Y background)")]
    public float cameraY = 0f;
    [Tooltip("Offset X nhân vật so với tâm camera")]
    public float characterOffsetX = 7f;

    [Header("Scene tiếp theo")]
    public string nextScene = "";

    [Header("Dialogue")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    [TextArea(2, 4)]
    public string[] dialogueLines;
    [Tooltip("Giây hiển thị mỗi dòng thoại")]
    public float dialogueShowDuration = 3f;
    [Tooltip("Giây ẩn giữa các dòng thoại")]
    public float dialogueGap = 2f;
    [Tooltip("Dịch bong bóng ngang trong canvas (âm = trái). Chỉnh để không bị cắt mép màn hình")]
    public float bubbleOffsetX = -100f;

    bool _triggered    = false;
    bool _dialogueDone = false;

    void Awake()
    {
        // HinhBongBong đã bị tắt trong scene — Awake chỉ là safety net
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        _triggered = true;
        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Tắt PlayerMovement
        if (character != null)
            foreach (var comp in character.GetComponents<MonoBehaviour>())
                if (comp.GetType().Name == "PlayerMovement") comp.enabled = false;

        // Nhân bản background
        float bgWidth = background.bounds.size.x;
        float originX = background.transform.position.x;
        float originY = background.transform.position.y;
        for (int i = 1; i < loopCount; i++)
        {
            SpriteRenderer copy = Instantiate(background, transform);
            copy.transform.position = new Vector3(originX + bgWidth * i, originY, background.transform.position.z);
        }

        // Tính điểm đầu/cuối scroll
        // centerX = tâm thật sự của toàn bộ dải background (originX là tâm tấm đầu tiên)
        float totalWidth = bgWidth * loopCount;
        float centerX   = originX + bgWidth * (loopCount - 1) / 2f;
        float camHalfW  = cam.orthographicSize * cam.aspect;
        float startX    = centerX + totalWidth / 2f - camHalfW;
        float endX      = centerX - totalWidth / 2f + camHalfW;
        cam.transform.position = new Vector3(startX, cameraY != 0 ? cameraY : originY, cam.transform.position.z);

        // Animation đi trái
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat("MoveX", -1f);
            characterAnimator.SetFloat("MoveY", 0f);
            characterAnimator.speed = 1f;
        }

        // Chạy dialogue song song với scroll
        _dialogueDone = false;
        StartCoroutine(DialogueSequence());

        // Cuộn camera + nhân vật theo — cho đến khi dialogue xong
        while (!_dialogueDone)
        {
            if (cam.transform.position.x > endX)
            {
                // Camera chưa dừng: cuộn và nhân vật bám theo camera
                Vector3 pos = cam.transform.position;
                pos.x = Mathf.Max(pos.x - scrollSpeed * Time.deltaTime, endX);
                cam.transform.position = pos;

                if (character != null)
                {
                    Vector3 cp = character.position;
                    cp.x = cam.transform.position.x + characterOffsetX;
                    character.position = cp;
                }
            }
            else
            {
                // Camera đã dừng tại endX: nhân vật đi tiếp độc lập sang trái
                if (character != null)
                {
                    Vector3 cp = character.position;
                    cp.x -= scrollSpeed * Time.deltaTime;
                    character.position = cp;
                }
            }

            yield return null;
        }

        // Sau thoại cuối: nhân vật đi sang trái đến khi khuất hẳn khỏi màn hình
        float offScreenX = endX - camHalfW; // cạnh trái màn hình khi cam khoá tại endX
        while (character != null && character.position.x > offScreenX)
        {
            Vector3 cp = character.position;
            cp.x -= scrollSpeed * Time.deltaTime;
            character.position = cp;
            yield return null;
        }

        if (characterAnimator != null) characterAnimator.speed = 0f;
        yield return new WaitForSeconds(0.5f);

        if (!string.IsNullOrEmpty(nextScene))
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextScene);
    }

    IEnumerator DialogueSequence()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) yield break;

        yield return new WaitForSeconds(1f); // delay trước dòng đầu

        foreach (string line in dialogueLines)
        {
            // Set text trước khi enable để TMP tính size ngay
            if (dialogueText != null)
            {
                dialogueText.text = line;
                dialogueText.ForceMeshUpdate();
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                var rt = dialoguePanel.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(bubbleOffsetX, rt.anchoredPosition.y);
            }

            // Chờ 1 frame để Unity xử lý OnEnable xong rồi mới rebuild layout
            yield return null;
            Canvas.ForceUpdateCanvases();
            if (dialoguePanel != null)
            {
                var rt = dialoguePanel.GetComponent<RectTransform>();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                // Căn cạnh phải bubble ngay tâm canvas (= đầu nhân vật), rồi fine-tune thêm bubbleOffsetX
                rt.anchoredPosition = new Vector2(-rt.rect.width / 2f + bubbleOffsetX, rt.anchoredPosition.y);
            }

            yield return new WaitForSeconds(dialogueShowDuration); // hiện đủ giây

            if (dialoguePanel != null) dialoguePanel.SetActive(false);

            yield return new WaitForSeconds(dialogueGap); // ẩn đủ giây
        }

        _dialogueDone = true;
    }
}
