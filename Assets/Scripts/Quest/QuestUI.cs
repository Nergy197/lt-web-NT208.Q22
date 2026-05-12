using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI cho hệ thống Quest.
/// Tự động hiển thị Quest Panel khi có quest active.
/// Tự động hiển thị Branch Panel khi quest có BranchChoices.
///
/// Setup trong Unity:
///   1. Tạo Canvas → Panel "QuestPanel" và Panel "BranchPanel"
///   2. Gắn script này vào bất kỳ GameObject nào trong scene
///   3. Kéo các UI element vào đúng field trong Inspector
/// </summary>
public class QuestUI : MonoBehaviour
{
    [Header("Quest Panel")]
    [Tooltip("Panel hiển thị quest đang active")]
    public GameObject questPanel;

    [Tooltip("Text hiển thị tên quest")]
    public TextMeshProUGUI questTitleText;

    [Tooltip("Text hiển thị danh sách objectives")]
    public TextMeshProUGUI objectivesText;

    [Header("Branch Choice Panel")]
    [Tooltip("Panel xuất hiện khi quest có BranchChoices")]
    public GameObject branchPanel;

    [Tooltip("Prefab nút lựa chọn (cần có Button + TextMeshProUGUI)")]
    public GameObject branchButtonPrefab;

    [Tooltip("Container chứa các nút branch")]
    public Transform branchButtonContainer;

    // ─── Runtime ─────────────────────────────────────────────────────────

    string _currentQuestId;
    readonly List<GameObject> _spawnedButtons = new();

    // ─── Lifecycle ───────────────────────────────────────────────────────

    void OnEnable()
    {
        QuestEvents.OnQuestStarted       += OnQuestStarted;
        QuestEvents.OnObjectiveCompleted += OnObjectiveCompleted;
        QuestEvents.OnQuestCompleted     += OnQuestCompleted;
    }

    void OnDisable()
    {
        QuestEvents.OnQuestStarted       -= OnQuestStarted;
        QuestEvents.OnObjectiveCompleted -= OnObjectiveCompleted;
        QuestEvents.OnQuestCompleted     -= OnQuestCompleted;

        // Đảm bảo Time.timeScale được restore nếu branch panel đang mở khi scene chuyển
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    // ─── Event handlers ──────────────────────────────────────────────────

    void OnQuestStarted(QuestSO quest)
    {
        if (quest.IsMainQuest)
        {
            _currentQuestId = quest.Id;
            RefreshQuestPanel(quest);
        }

        // Tự detect: hiện Branch Panel nếu quest có lựa chọn
        if (quest.BranchChoices != null && quest.BranchChoices.Count > 0)
            ShowBranchPanel(quest);
    }

    void OnObjectiveCompleted(string questId, string objectiveId)
    {
        if (questId != _currentQuestId) return;
        var quest = QuestManager.Instance?.GetActiveQuest(questId);
        if (quest != null) RefreshQuestPanel(quest);
    }

    void OnQuestCompleted(QuestSO quest)
    {
        if (quest.Id == _currentQuestId)
            SetQuestPanelVisible(false);
    }

    // ─── Quest Panel ─────────────────────────────────────────────────────

    void RefreshQuestPanel(QuestSO quest)
    {
        if (questPanel == null) return;
        SetQuestPanelVisible(true);

        if (questTitleText != null)
            questTitleText.text = quest.IsMainQuest ? $"⚔ {quest.Title}" : $"◆ {quest.Title}";

        if (objectivesText == null) return;

        var sb = new System.Text.StringBuilder();
        foreach (var obj in quest.Objectives)
        {
            string icon     = obj.IsCompleted ? "<color=#88FF88>✔</color>" : "◻";
            string strike   = obj.IsCompleted ? "<s>" : "";
            string strikeEnd = obj.IsCompleted ? "</s>" : "";
            sb.AppendLine($"{icon} {strike}{obj.Description}{strikeEnd}");
        }
        objectivesText.text = sb.ToString();
    }

    void SetQuestPanelVisible(bool visible)
    {
        if (questPanel != null) questPanel.SetActive(visible);
    }

    // ─── Branch Panel ────────────────────────────────────────────────────

    void ShowBranchPanel(QuestSO quest)
    {
        if (branchPanel == null || branchButtonPrefab == null) return;

        foreach (var btn in _spawnedButtons) Destroy(btn);
        _spawnedButtons.Clear();

        foreach (var branch in quest.BranchChoices)
        {
            var go  = Instantiate(branchButtonPrefab, branchButtonContainer);
            var btn = go.GetComponentInChildren<Button>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null) txt.text = branch.Description;

            string capturedBranchId = branch.Id;
            string capturedQuestId  = quest.Id;

            if (btn != null)
                btn.onClick.AddListener(() => OnBranchSelected(capturedQuestId, capturedBranchId));

            _spawnedButtons.Add(go);
        }

        branchPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void OnBranchSelected(string questId, string branchId)
    {
        branchPanel.SetActive(false);
        Time.timeScale = 1f;
        QuestManager.Instance?.ChooseBranch(questId, branchId);
    }
}
