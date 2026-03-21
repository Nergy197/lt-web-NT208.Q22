using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa một quest.
/// Tạo asset: chuột phải trong Project > Create > Quests > New Quest
/// </summary>
[CreateAssetMenu(menuName = "Quests/New Quest", fileName = "NewQuest")]
public class QuestSO : ScriptableObject
{
    [Header("Identity")]
    public string Id;
    public string Title;
    [TextArea] public string Description;
    public bool IsMainQuest;

    [Header("Objectives")]
    public List<QuestObjective> Objectives = new();

    [Header("Branch Choices")]
    [Tooltip("Nếu có, quest này sẽ hiển thị panel cho người chơi chọn nhánh.")]
    public List<QuestBranchChoice> BranchChoices = new();

    [Header("Progression")]
    [Tooltip("Các quest tự động mở sau khi hoàn thành quest này. Kéo SO asset vào đây.")]
    public List<QuestSO> NextQuests = new();

    // ─── Runtime state ───────────────────────────────────────────────────

    public bool IsCompleted
    {
        get
        {
            if (Objectives == null || Objectives.Count == 0) return false;
            foreach (var obj in Objectives)
                if (!obj.IsCompleted) return false;
            return true;
        }
    }

    // ─── API ─────────────────────────────────────────────────────────────

    public void CompleteObjective(string objectiveId)
    {
        foreach (var obj in Objectives)
        {
            if (obj.Id != objectiveId || obj.IsCompleted) continue;

            obj.IsCompleted = true;
            Debug.Log($"[QUEST] Objective «{objectiveId}» completed in quest «{Id}»");
            QuestEvents.RaiseObjectiveCompleted(Id, objectiveId);

            if (IsCompleted)
            {
                Debug.Log($"[QUEST] Quest «{Id}» ({Title}) COMPLETED");
                QuestEvents.RaiseQuestCompleted(this);
            }
            return;
        }

        Debug.LogWarning($"[QUEST] Objective «{objectiveId}» not found or already done in quest «{Id}»");
    }

    public void ResetObjectives()
    {
        foreach (var obj in Objectives)
            obj.IsCompleted = false;
    }

    public void ApplySaveData(QuestProgressData data)
    {
        if (data?.Objectives == null) return;

        foreach (var saved in data.Objectives)
            foreach (var obj in Objectives)
                if (obj.Id == saved.Id) { obj.IsCompleted = saved.IsCompleted; break; }
    }
}
