using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa một quest (main hoặc side).
/// Tạo asset mới: chuột phải trong Project > Create > Quests > ...
/// </summary>
[CreateAssetMenu(menuName = "Quests/New Quest", fileName = "NewQuest")]
public class QuestSO : ScriptableObject
{
    // ─── Data ────────────────────────────────────────────────────────────

    [Header("Identity")]
    public string Id;
    public string Title;
    [TextArea] public string Description;
    public bool IsMainQuest;

    [Header("Objectives")]
    public List<QuestObjective> Objectives = new();

    [Header("Branch Choices")]
    public List<QuestBranchChoice> BranchChoices = new();

    // ─── Runtime state ───────────────────────────────────────────────────

    /// <summary>True khi mọi objective đã IsCompleted.</summary>
    public bool IsCompleted
    {
        get
        {
            if (Objectives == null || Objectives.Count == 0)
                return false;

            foreach (var obj in Objectives)
                if (!obj.IsCompleted) return false;

            return true;
        }
    }

    // ─── API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Đánh dấu một objective hoàn thành và phát event tương ứng.
    /// Nếu tất cả objectives xong → phát OnQuestCompleted.
    /// </summary>
    public void CompleteObjective(string objectiveId)
    {
        foreach (var obj in Objectives)
        {
            if (obj.Id != objectiveId || obj.IsCompleted)
                continue;

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

    /// <summary>Reset trạng thái runtime (dùng khi load lại scene).</summary>
    public void ResetObjectives()
    {
        foreach (var obj in Objectives)
            obj.IsCompleted = false;
    }
}
