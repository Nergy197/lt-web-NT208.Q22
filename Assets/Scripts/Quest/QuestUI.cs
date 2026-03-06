using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI cho hệ thống Quest.
/// Hiển thị: tên quest active, danh sách objectives, và Branch Choice Panel khi Q005 bắt đầu.
///
/// Setup trong Unity:
///   1. Tạo Canvas → tạo Panel "QuestPanel" và Panel "BranchPanel"
///   2. Gắn script này vào bất kỳ GameObject nào trong scene
///   3. Kéo các UI element vào đúng field trong Inspector
/// </summary>
public class QuestUI : MonoBehaviour
{
    // ─── Inspector ───────────────────────────────────────────────────────

    [Header("Quest Panel")]
    [Tooltip("Panel hiển thị quest đang active (title + objectives)")]
    public GameObject questPanel;

    [Tooltip("Text hiển thị tên quest")]
    public TextMeshProUGUI questTitleText;

    [Tooltip("Text hiển thị danh sách objectives")]
    public TextMeshProUGUI objectivesText;

    [Header("Branch Choice Panel")]
    [Tooltip("Panel xuất hiện khi Q005 bắt đầu")]
    public GameObject branchPanel;

    [Tooltip("Prefab của 1 nút lựa chọn branch. Phải có Button + TextMeshProUGUI.")]
    public GameObject branchButtonPrefab;

    [Tooltip("Container chứa các nút branch")]
    public Transform branchButtonContainer;

    [Header("Settings")]
    [Tooltip("Id của quest sẽ trigger Branch Panel (mặc định Q005)")]
    public string branchingQuestId = "Q005";

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
    }

    // ─── Event handlers ──────────────────────────────────────────────────

    void OnQuestStarted(QuestSO quest)
    {
        // Hiện panel với main quest mới nhất
        if (quest.IsMainQuest)
        {
            _currentQuestId = quest.Id;
            RefreshQuestPanel(quest);
        }

        // Quest Q005 → hiện Branch Panel
        if (quest.Id == branchingQuestId)
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
        // Ẩn quest panel tạm khi quest hoàn thành (quest mới sẽ raise OnQuestStarted ngay sau)
        if (quest.Id == _currentQuestId)
            SetQuestPanelVisible(false);
    }

    // ─── Quest Panel ─────────────────────────────────────────────────────

    void RefreshQuestPanel(QuestSO quest)
    {
        if (questPanel == null) return;

        SetQuestPanelVisible(true);

        if (questTitleText != null)
            questTitleText.text = quest.IsMainQuest
                ? $"⚔ {quest.Title}"
                : $"◆ {quest.Title}";

        if (objectivesText == null) return;

        var sb = new System.Text.StringBuilder();
        foreach (var obj in quest.Objectives)
        {
            string icon = obj.IsCompleted ? "<color=#88FF88>✔</color>" : "◻";
            string style = obj.IsCompleted ? "<s>" : "";
            string endStyle = obj.IsCompleted ? "</s>" : "";
            sb.AppendLine($"{icon} {style}{obj.Description}{endStyle}");
        }
        objectivesText.text = sb.ToString();
    }

    void SetQuestPanelVisible(bool visible)
    {
        if (questPanel != null)
            questPanel.SetActive(visible);
    }

    // ─── Branch Panel ────────────────────────────────────────────────────

    void ShowBranchPanel(QuestSO quest)
    {
        if (branchPanel == null || branchButtonPrefab == null) return;

        // Xoá nút cũ
        foreach (var btn in _spawnedButtons)
            Destroy(btn);
        _spawnedButtons.Clear();

        // Tạo nút mới cho từng branch
        foreach (var branch in quest.BranchChoices)
        {
            var go  = Instantiate(branchButtonPrefab, branchButtonContainer);
            var btn = go.GetComponentInChildren<Button>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();

            if (txt != null) txt.text = branch.Description;

            // Capture để dùng trong lambda
            string capturedBranchId = branch.Id;
            string capturedQuestId  = quest.Id;

            if (btn != null)
                btn.onClick.AddListener(() => OnBranchSelected(capturedQuestId, capturedBranchId));

            _spawnedButtons.Add(go);
        }

        branchPanel.SetActive(true);

        // Tạm dừng game khi đang chọn nhánh (nếu dùng Time.timeScale)
        Time.timeScale = 0f;
    }

    void OnBranchSelected(string questId, string branchId)
    {
        // Ẩn panel
        branchPanel.SetActive(false);
        Time.timeScale = 1f;

        // Gọi QuestManager xử lý
        QuestManager.Instance?.ChooseBranch(questId, branchId);
    }
}
