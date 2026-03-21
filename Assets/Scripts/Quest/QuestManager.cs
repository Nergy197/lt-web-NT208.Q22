using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ vòng đời quest.
/// Gắn vào một GameObject trong scene đầu (DontDestroyOnLoad).
///
/// Setup:
///   1. Kéo các quest SO muốn bắt đầu ngay vào StartingQuests
///   2. Nhấn chuột phải component > "Collect All Quests From Chain" để tự điền AllQuests
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    [Header("Quest Database")]
    [Tooltip("Toàn bộ QuestSO cần biết để save/load. Nhấn chuột phải > Collect để tự điền.")]
    public List<QuestSO> AllQuests = new();

    [Header("Starting Quests")]
    [Tooltip("Các quest tự động bắt đầu khi vào game. Kéo SO asset vào đây.")]
    public List<QuestSO> StartingQuests = new();

    public List<QuestSO> ActiveQuests    { get; private set; } = new();
    public List<QuestSO> CompletedQuests { get; private set; } = new();

    private bool _loadedFromServer = false;

    void Start()
    {
        QuestEvents.OnQuestCompleted += HandleQuestCompleted;

        // Không start ở đây — GameManager gọi LoadProgress rồi gọi StartNewGame nếu cần
    }

    void OnDestroy() => QuestEvents.OnQuestCompleted -= HandleQuestCompleted;

    void OnApplicationQuit()          => SaveProgress();
    void OnApplicationPause(bool p)   { if (p) SaveProgress(); }

    // ─── New Game ────────────────────────────────────────────────────────

    /// <summary>Bắt đầu game mới: start tất cả StartingQuests.</summary>
    public void StartNewGame()
    {
        foreach (var q in StartingQuests)
            if (q != null) StartQuest(q);
    }

    // ─── Quest Lifecycle ─────────────────────────────────────────────────

    /// <summary>Bắt đầu quest bằng SO reference trực tiếp.</summary>
    public void StartQuest(QuestSO quest)
    {
        if (quest == null) return;

        if (ActiveQuests.Contains(quest) || CompletedQuests.Contains(quest))
        {
            Debug.Log($"[QUEST] «{quest.Id}» đã active/completed, bỏ qua.");
            return;
        }

        quest.ResetObjectives();
        ActiveQuests.Add(quest);
        Debug.Log($"[QUEST] Started: {quest.Title} ({quest.Id})");
        QuestEvents.RaiseQuestStarted(quest);
    }

    /// <summary>Bắt đầu quest bằng Id (dùng cho save/load hoặc gọi từ code cũ).</summary>
    public void StartQuest(string questId)
    {
        var quest = FindQuest(questId);
        if (quest == null) { Debug.LogWarning($"[QUEST] StartQuest: không tìm thấy Id=«{questId}»"); return; }
        StartQuest(quest);
    }

    /// <summary>Hoàn thành một objective. Nếu quest xong → auto-chain sang NextQuests.</summary>
    public void CompleteObjective(string questId, string objectiveId)
    {
        var quest = GetActiveQuest(questId);
        if (quest == null) { Debug.LogWarning($"[QUEST] CompleteObjective: «{questId}» chưa active."); return; }

        quest.CompleteObjective(objectiveId);

        if (quest.IsCompleted)
            MoveToCompleted(quest);
    }

    /// <summary>Người chơi chọn một nhánh trong quest có BranchChoices.</summary>
    public void ChooseBranch(string questId, string branchId)
    {
        var quest = GetActiveQuest(questId);
        if (quest == null) { Debug.LogWarning($"[QUEST] ChooseBranch: «{questId}» chưa active."); return; }

        foreach (var branch in quest.BranchChoices)
        {
            if (branch.Id != branchId) continue;

            Debug.Log($"[QUEST] Branch «{branchId}» chosen → next: «{branch.LeadsToQuest?.Id}»");

            foreach (var obj in quest.Objectives) obj.IsCompleted = true;
            MoveToCompleted(quest);
            QuestEvents.RaiseQuestCompleted(quest);

            if (branch.LeadsToQuest != null)
                StartQuest(branch.LeadsToQuest);
            return;
        }

        Debug.LogWarning($"[QUEST] ChooseBranch: branch «{branchId}» không tìm thấy trong «{questId}»");
    }

    // ─── Auto-chain ──────────────────────────────────────────────────────

    void HandleQuestCompleted(QuestSO quest)
    {
        foreach (var next in quest.NextQuests)
            if (next != null) StartQuest(next);
    }

    // ─── Queries ─────────────────────────────────────────────────────────

    public QuestSO GetActiveQuest(string questId)
    {
        foreach (var q in ActiveQuests) if (q.Id == questId) return q;
        return null;
    }

    public bool IsQuestCompleted(string questId)
    {
        foreach (var q in CompletedQuests) if (q.Id == questId) return true;
        return false;
    }

    public bool IsObjectiveCompleted(string questId, string objectiveId)
    {
        var quest = FindQuest(questId);
        if (quest == null) return false;
        foreach (var obj in quest.Objectives) if (obj.Id == objectiveId) return obj.IsCompleted;
        return false;
    }

    // ─── Save / Load ─────────────────────────────────────────────────────

    public const string SaveKey = "QuestSave";

    public AllQuestsSaveData BuildSaveData()
    {
        var data = new AllQuestsSaveData();
        foreach (var q in ActiveQuests)    data.Quests.Add(BuildProgressData(q, "Active"));
        foreach (var q in CompletedQuests) data.Quests.Add(BuildProgressData(q, "Completed"));
        return data;
    }

    public void SaveProgress()
    {
        var data = BuildSaveData();
        PlayerPrefs.SetString(SaveKey, UnityEngine.JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        Debug.Log($"[QUEST] Saved {data.Quests.Count} quests.");
    }

    public void LoadProgressFromData(AllQuestsSaveData saveData)
    {
        if (saveData?.Quests == null || saveData.Quests.Count == 0) return;
        ApplySaveData(saveData);
        _loadedFromServer = true;
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) { Debug.Log("[QUEST] Không có save – bắt đầu mới."); return; }

        var data = UnityEngine.JsonUtility.FromJson<AllQuestsSaveData>(PlayerPrefs.GetString(SaveKey));
        if (data?.Quests == null) { Debug.LogWarning("[QUEST] Save data lỗi."); return; }

        ApplySaveData(data);
    }

    void ApplySaveData(AllQuestsSaveData saveData)
    {
        ActiveQuests.Clear();
        CompletedQuests.Clear();

        foreach (var d in saveData.Quests)
        {
            var quest = FindQuest(d.QuestId);
            if (quest == null) { Debug.LogWarning($"[QUEST] Load: không tìm thấy «{d.QuestId}»"); continue; }

            quest.ResetObjectives();
            quest.ApplySaveData(d);

            if (d.Status == "Active")      { ActiveQuests.Add(quest);    QuestEvents.RaiseQuestStarted(quest); }
            else if (d.Status == "Completed") CompletedQuests.Add(quest);
        }
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    // ─── Editor Helper ───────────────────────────────────────────────────

    /// <summary>
    /// Tự động duyệt toàn bộ chain (từ StartingQuests) và điền vào AllQuests.
    /// Nhấn chuột phải vào component trong Inspector để chạy.
    /// </summary>
    [ContextMenu("Collect All Quests From Chain")]
    void CollectAllQuests()
    {
        var visited = new System.Collections.Generic.HashSet<string>();
        var queue   = new System.Collections.Generic.Queue<QuestSO>();

        foreach (var q in StartingQuests) if (q != null) queue.Enqueue(q);

        AllQuests.Clear();

        while (queue.Count > 0)
        {
            var q = queue.Dequeue();
            if (q == null || visited.Contains(q.Id)) continue;

            visited.Add(q.Id);
            AllQuests.Add(q);

            foreach (var next in q.NextQuests)
                if (next != null) queue.Enqueue(next);

            foreach (var branch in q.BranchChoices)
                if (branch.LeadsToQuest != null) queue.Enqueue(branch.LeadsToQuest);
        }

        Debug.Log($"[QUEST] Collected {AllQuests.Count} quests from chain.");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // ─── Internal ────────────────────────────────────────────────────────

    QuestSO FindQuest(string questId)
    {
        foreach (var q in AllQuests) if (q != null && q.Id == questId) return q;
        return null;
    }

    void MoveToCompleted(QuestSO quest)
    {
        if (!ActiveQuests.Contains(quest)) return;
        ActiveQuests.Remove(quest);
        if (!CompletedQuests.Contains(quest)) CompletedQuests.Add(quest);
        Debug.Log($"[QUEST] «{quest.Id}» → Completed.");
    }

    static QuestProgressData BuildProgressData(QuestSO quest, string status)
    {
        var data = new QuestProgressData { QuestId = quest.Id, Status = status };
        foreach (var obj in quest.Objectives)
            data.Objectives.Add(new ObjectiveSaveData { Id = obj.Id, IsCompleted = obj.IsCompleted });
        return data;
    }
}
