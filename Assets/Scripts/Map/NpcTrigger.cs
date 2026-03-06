using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào GameObject NPC trên MapScene.
/// Khi player đến gần và nhấn F → hiện dialogue → trigger quest objective.
///
/// Setup:
///   1. Tạo NpcData asset (chuột phải → Create → NPC → NPC Data)
///   2. Gắn script này vào GameObject NPC
///   3. Gắn BoxCollider2D với IsTrigger = true
///   4. Kéo NpcData vào field "npc Data"
///   5. (Tuỳ chọn) Gắn dialoguePanel vào một Text UI trong Canvas
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NpcTrigger : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────

    [Header("NPC Config")]
    public NpcData npcData;

    [Tooltip("Nếu true, chỉ có thể nói chuyện 1 lần (quest dialogue). Nếu false, lặp lại mỗi lần.")]
    public bool triggerOnce = true;

    [Header("UI (tuỳ chọn)")]
    [Tooltip("Panel hiện dialogue. Để trống nếu chỉ dùng Debug.Log.")]
    public GameObject dialoguePanel;

    [Tooltip("Text hiển thị tên NPC.")]
    public TextMeshProUGUI nameText;

    [Tooltip("Text hiển thị nội dung hội thoại.")]
    public TextMeshProUGUI dialogueText;

    [Tooltip("Prompt 'Nhấn F' hiển thị khi player trong vùng.")]
    public GameObject interactPrompt;

    // ─── Runtime ─────────────────────────────────────────────────────────

    bool _triggeredOnce = false;
    bool _inRange       = false;
    bool _isTalking     = false;
    int  _lineIndex     = 0;

    // ─── Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        HideDialogue();
        SetPromptVisible(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _inRange = true;

        if (_triggeredOnce && triggerOnce) return;

        SetPromptVisible(true);
        Debug.Log($"[NPC] Nhấn F để nói chuyện với {npcData?.npcName}");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _inRange = false;
        SetPromptVisible(false);

        if (_isTalking)
            EndDialogue();
    }

    void Update()
    {
        if (!_inRange) return;
        if (_triggeredOnce && triggerOnce && !_isTalking) return;
        if (InputController.Instance == null) return;

        if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
        {
            if (!_isTalking)
                StartDialogue();
            else
                AdvanceDialogue();
        }
    }

    // ─── Dialogue flow ───────────────────────────────────────────────────

    void StartDialogue()
    {
        if (npcData == null) return;

        _isTalking  = true;
        _lineIndex  = 0;

        // Nếu đã nói rồi và không triggerOnce → dùng repeatLine
        bool useRepeat = _triggeredOnce && !triggerOnce;

        SetPromptVisible(false);
        ShowDialogue(npcData.npcName,
            useRepeat ? npcData.repeatLine : GetCurrentLine());

        Debug.Log($"[NPC] {npcData.npcName}: {(useRepeat ? npcData.repeatLine : GetCurrentLine())}");
    }

    void AdvanceDialogue()
    {
        _lineIndex++;

        bool useRepeat = _triggeredOnce && !triggerOnce;

        if (!useRepeat && _lineIndex < npcData.dialogueLines.Length)
        {
            // Còn dòng tiếp
            ShowDialogue(npcData.npcName, GetCurrentLine());
            Debug.Log($"[NPC] {npcData.npcName}: {GetCurrentLine()}");
        }
        else
        {
            // Hết dialogue
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        _isTalking = false;
        HideDialogue();

        // Chỉ trigger quest lần đầu
        if (!_triggeredOnce)
        {
            _triggeredOnce = true;
            TriggerQuestObjective();
        }
    }

    string GetCurrentLine()
    {
        if (npcData.dialogueLines == null || npcData.dialogueLines.Length == 0)
            return "...";
        return npcData.dialogueLines[Mathf.Clamp(_lineIndex, 0, npcData.dialogueLines.Length - 1)];
    }

    // ─── Quest integration ───────────────────────────────────────────────

    void TriggerQuestObjective()
    {
        if (string.IsNullOrEmpty(npcData.questId)) return;

        Debug.Log($"[NPC] Trigger quest: {npcData.questId} / {npcData.objectiveId}");
        QuestManager.Instance?.CompleteObjective(npcData.questId, npcData.objectiveId);
    }

    // ─── UI helpers ──────────────────────────────────────────────────────

    void ShowDialogue(string speaker, string line)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (nameText     != null) nameText.text     = speaker;
        if (dialogueText != null) dialogueText.text = line;
    }

    void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nameText      != null) nameText.text     = "";
        if (dialogueText  != null) dialogueText.text = "";
    }

    void SetPromptVisible(bool visible)
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(visible);
    }
}
