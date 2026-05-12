using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gắn vào GameObject NPC trên MapScene.
/// Khi player đến gần và nhấn F → hiện dialogue → kích hoạt Quest Actions.
///
/// Setup:
///   1. Tạo NpcData asset (chuột phải → Create → NPC → NPC Data) và điền tên + thoại
///   2. Gắn script này vào GameObject NPC
///   3. Gắn BoxCollider2D với IsTrigger = true
///   4. Kéo NpcData vào field "Npc Data"
///   5. Thêm Quest Actions: kéo QuestSO, chọn TriggerOn = OnDialogueEnd
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NpcTrigger : MonoBehaviour
{
    [Header("NPC Config")]
    public NpcData npcData;

    [Tooltip("Nếu true, chỉ trigger quest 1 lần duy nhất.")]
    public bool triggerOnce = true;

    [Header("UI (tuỳ chọn)")]
    public GameObject            dialoguePanel;
    public TextMeshProUGUI       nameText;
    public TextMeshProUGUI       dialogueText;
    public GameObject            interactPrompt;

    [Header("Quest Actions")]
    [Tooltip("Kích hoạt khi dialogue kết thúc.\n" +
             "Kéo QuestSO vào Quest, chọn TriggerOn = OnDialogueEnd.")]
    public List<QuestAction> questActions = new();

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
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _inRange = false;
        SetPromptVisible(false);
        if (_isTalking) EndDialogue();
    }

    void Update()
    {
        if (!_inRange) return;
        if (_triggeredOnce && triggerOnce && !_isTalking) return;
        if (InputController.Instance == null) return;

        // Chỉ xử lý khi đang ở chế độ Map (tránh xung đột với UI/Battle)
        if (InputController.Instance.Mode != InputMode.Map) return;

        if (InputController.Instance.Input.Map.Interact.WasPressedThisFrame())
        {
            if (!_isTalking) StartDialogue();
            else             AdvanceDialogue();
        }
    }

    // ─── Dialogue flow ───────────────────────────────────────────────────

    void StartDialogue()
    {
        if (npcData == null) return;
        _isTalking = true;
        _lineIndex = 0;
        SetPromptVisible(false);

        bool useRepeat = _triggeredOnce && !triggerOnce;
        ShowDialogue(npcData.npcName, useRepeat ? npcData.repeatLine : GetCurrentLine());
    }

    void AdvanceDialogue()
    {
        _lineIndex++;
        bool useRepeat = _triggeredOnce && !triggerOnce;

        if (!useRepeat && _lineIndex < npcData.dialogueLines.Length)
            ShowDialogue(npcData.npcName, GetCurrentLine());
        else
            EndDialogue();
    }

    void EndDialogue()
    {
        _isTalking = false;
        HideDialogue();

        if (!_triggeredOnce)
        {
            _triggeredOnce = true;
            QuestAction.Execute(questActions, QuestAction.When.OnDialogueEnd);
        }
    }

    string GetCurrentLine()
    {
        if (npcData.dialogueLines == null || npcData.dialogueLines.Length == 0) return "...";
        return npcData.dialogueLines[Mathf.Clamp(_lineIndex, 0, npcData.dialogueLines.Length - 1)];
    }

    // ─── UI helpers ──────────────────────────────────────────────────────

    void ShowDialogue(string speaker, string line)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (nameText      != null) nameText.text      = speaker;
        if (dialogueText  != null) dialogueText.text  = line;
    }

    void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (nameText      != null) nameText.text      = "";
        if (dialogueText  != null) dialogueText.text  = "";
    }

    void SetPromptVisible(bool visible)
    {
        if (interactPrompt != null) interactPrompt.SetActive(visible);
    }
}
