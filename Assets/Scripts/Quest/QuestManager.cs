using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý toàn bộ vòng đời quest trong game.
/// Gắn component này vào một GameObject trong scene (DontDestroyOnLoad).
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Quest Database")]
    [Tooltip("Kéo tất cả QuestSO assets vào đây. Hoặc để trống và dùng chapter1Data.")]
    public List<QuestSO> allQuests = new();

    [Header("Chapter 1")]
    [Tooltip("Kéo Chapter1QuestData.asset vào đây để tự động load dữ liệu Chapter 1.")]
    public Chapter1QuestData chapter1Data;

    /// <summary>Danh sách quest đang active (đã StartQuest, chưa completed).</summary>
    public List<QuestSO> ActiveQuests { get; private set; } = new();

    /// <summary>Danh sách quest đã hoàn thành.</summary>
    public List<QuestSO> CompletedQuests { get; private set; } = new();

    void Start()
    {
        // Nếu có Chapter1QuestData, tự động đăng ký tất cả quest Chapter 1
        if (chapter1Data != null)
        {
            var ch1Quests = chapter1Data.BuildQuests();
            foreach (var q in ch1Quests)
                if (!allQuests.Exists(x => x.Id == q.Id))
                    allQuests.Add(q);

            Debug.Log($"[QUEST] Chapter 1 data loaded: {ch1Quests.Count} quests registered.");
        }

        // Subscribe auto-progression
        QuestEvents.OnQuestCompleted += HandleQuestCompleted;

        // Load tiến độ đã lưu (sau khi allQuests đã được populate)
        LoadProgress();
    }

    void OnDestroy()
    {
        QuestEvents.OnQuestCompleted -= HandleQuestCompleted;
    }

    /// <summary>Tự động lưu khi thoát game (PC / Editor).</summary>
    void OnApplicationQuit()
    {
        SaveProgress();
    }

    /// <summary>Tự động lưu khi game bị minimize / chuyển app (mobile).</summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveProgress();
    }

    /// <summary>
    /// Tự động kích hoạt quest tiếp theo khi một quest hoàn thành.
    /// Đây là logic tuyến tính của Chapter 1.
    /// </summary>
    void HandleQuestCompleted(QuestSO quest)
    {
        switch (quest.Id)
        {
            case "Q001":
                // Di nguyện → Bước vào chiến tranh
                StartQuest("Q002");
                break;

            case "Q002":
                // Bước vào chiến tranh → Thất bại đầu tiên
                StartQuest("Q003");
                break;

            case "Q003":
                // Thất bại đầu tiên → Đất nước trong tay + Side quest mở song song
                StartQuest("Q004");
                StartQuest("Q010"); // Side quest: Hậu quả chiến tranh
                break;

            case "Q004":
                // Đất nước trong tay → Ngã ba số phận
                StartQuest("Q005");
                break;

            // Q005 (Ngã ba) kết thúc khi người chơi ChooseBranch → Q006 hoặc Q007
            // Q010 (Side quest) không auto-chain, tự kết thúc
        }
    }

    /// <summary>
    /// Bắt đầu quest theo Id. Nếu quest đã active hoặc completed → bỏ qua.
    /// </summary>
    public void StartQuest(string questId)
    {
        var quest = FindQuest(questId);

        if (quest == null)
        {
            Debug.LogWarning($"[QUEST] StartQuest: không tìm thấy quest Id=«{questId}»");
            return;
        }

        if (ActiveQuests.Contains(quest) || CompletedQuests.Contains(quest))
        {
            Debug.Log($"[QUEST] Quest «{questId}» đã active hoặc đã completed, bỏ qua.");
            return;
        }

        quest.ResetObjectives();
        ActiveQuests.Add(quest);
        Debug.Log($"[QUEST] Started: {quest.Title} (Id={quest.Id})");
        QuestEvents.RaiseQuestStarted(quest);
    }

    /// <summary>
    /// Đánh dấu một objective hoàn thành.
    /// Nếu tất cả objectives của quest xong → chuyển quest sang CompletedQuests.
    /// </summary>
    public void CompleteObjective(string questId, string objectiveId)
    {
        var quest = GetActiveQuest(questId);

        if (quest == null)
        {
            Debug.LogWarning($"[QUEST] CompleteObjective: quest «{questId}» chưa active.");
            return;
        }

        quest.CompleteObjective(objectiveId);

        if (quest.IsCompleted)
            MoveToCompleted(quest);
    }

    /// <summary>
    /// Xử lý khi người chơi chọn một nhánh (branch).
    /// Quest có LeadsToQuestId sẽ được StartQuest và Q005 được đánh dấu completed.
    /// </summary>
    public void ChooseBranch(string questId, string branchId)
    {
        var quest = FindQuest(questId);

        if (quest == null)
        {
            Debug.LogWarning($"[QUEST] ChooseBranch: không tìm thấy quest «{questId}»");
            return;
        }

        foreach (var branch in quest.BranchChoices)
        {
            if (branch.Id != branchId) continue;

            Debug.Log($"[QUEST] Branch «{branchId}» chosen → next quest «{branch.LeadsToQuestId}»");

            // Hoàn thành Q005 trước khi bắt đầu quest nhánh
            CompleteObjective(questId, "O1");
            MoveToCompleted(quest);

            StartQuest(branch.LeadsToQuestId);
            return;
        }

        Debug.LogWarning($"[QUEST] ChooseBranch: branch «{branchId}» không tìm thấy trong quest «{questId}»");
    }

    /// <summary>Lấy quest đang active theo Id. Trả về null nếu không tìm thấy.</summary>
    public QuestSO GetActiveQuest(string questId)
    {
        foreach (var q in ActiveQuests)
            if (q.Id == questId) return q;
        return null;
    }

    /// <summary>Kiểm tra quest đã hoàn thành chưa (dùng cho UI/dialogue).</summary>
    public bool IsQuestCompleted(string questId)
    {
        foreach (var q in CompletedQuests)
            if (q.Id == questId) return true;
        return false;
    }

    /// <summary>Kiểm tra một objective cụ thể đã done chưa (dùng cho dialogue branching).</summary>
    public bool IsObjectiveCompleted(string questId, string objectiveId)
    {
        var quest = FindQuest(questId);
        if (quest == null) return false;

        foreach (var obj in quest.Objectives)
            if (obj.Id == objectiveId) return obj.IsCompleted;

        return false;
    }

    /// <summary>PlayerPrefs key lưu toàn bộ tiến độ quest.</summary>
    public const string SaveKey = "QuestSave";

    /// <summary>
    /// Serialize tiến độ hiện tại (ActiveQuests + CompletedQuests) ra JSON
    /// và lưu vào PlayerPrefs.
    /// </summary>
    public void SaveProgress()
    {
        var saveData = new AllQuestsSaveData();

        foreach (var quest in ActiveQuests)
            saveData.Quests.Add(BuildProgressData(quest, "Active"));

        foreach (var quest in CompletedQuests)
            saveData.Quests.Add(BuildProgressData(quest, "Completed"));

        var json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"[QUEST] Progress saved ({saveData.Quests.Count} quests).");
    }

    /// <summary>
    /// Đọc JSON từ PlayerPrefs và restore ActiveQuests / CompletedQuests.
    /// Phải gọi sau khi allQuests đã được populate.
    /// </summary>
    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Debug.Log("[QUEST] Không có save data – bắt đầu mới.");
            return;
        }

        var json     = PlayerPrefs.GetString(SaveKey);
        var saveData = JsonUtility.FromJson<AllQuestsSaveData>(json);

        if (saveData?.Quests == null)
        {
            Debug.LogWarning("[QUEST] Save data bị lỗi, bỏ qua.");
            return;
        }

        // Xóa trạng thái cũ trước khi restore
        ActiveQuests.Clear();
        CompletedQuests.Clear();

        foreach (var data in saveData.Quests)
        {
            var quest = FindQuest(data.QuestId);
            if (quest == null)
            {
                Debug.LogWarning($"[QUEST] LoadProgress: quest «{data.QuestId}» không tìm thấy trong allQuests.");
                continue;
            }

            quest.ResetObjectives();      // đặt lại về false trước
            quest.ApplySaveData(data);    // restore đúng từng flag

            if (data.Status == "Active")
                ActiveQuests.Add(quest);
            else if (data.Status == "Completed")
                CompletedQuests.Add(quest);
        }

        Debug.Log($"[QUEST] Progress loaded: {ActiveQuests.Count} active, {CompletedQuests.Count} completed.");
    }

    /// <summary>Xóa save data (dùng cho New Game).</summary>
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[QUEST] Save data đã được xóa.");
    }

    static QuestProgressData BuildProgressData(QuestSO quest, string status)
    {
        var data = new QuestProgressData
        {
            QuestId    = quest.Id,
            Status     = status,
            Objectives = new List<ObjectiveSaveData>(),
        };

        foreach (var obj in quest.Objectives)
            data.Objectives.Add(new ObjectiveSaveData { Id = obj.Id, IsCompleted = obj.IsCompleted });

        return data;
    }

    QuestSO FindQuest(string questId)
    {
        foreach (var q in allQuests)
            if (q != null && q.Id == questId) return q;
        return null;
    }

    void MoveToCompleted(QuestSO quest)
    {
        if (!ActiveQuests.Contains(quest)) return;
        ActiveQuests.Remove(quest);
        if (!CompletedQuests.Contains(quest))
            CompletedQuests.Add(quest);
        Debug.Log($"[QUEST] Quest «{quest.Id}» moved to CompletedQuests.");
    }
}
