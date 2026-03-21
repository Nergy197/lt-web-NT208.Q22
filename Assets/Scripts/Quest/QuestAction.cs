using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một hành động quest được cấu hình trực tiếp trong Inspector.
/// Kéo QuestSO vào field Quest, chọn When và Action từ dropdown.
/// </summary>
[Serializable]
public class QuestAction
{
    public enum When
    {
        OnDialogueEnd,  // Cuối hội thoại NPC hoặc cutscene
        OnEnterZone,    // Player bước vào vùng (Collider2D trigger)
        OnExitZone,     // Player rời khỏi vùng
        OnBattleWin,    // Thắng trận
        OnBattleLoss,   // Thua trận
        Manual,         // Gọi thủ công qua code: QuestAction.Execute(actions, When.Manual)
    }

    public enum ActionType
    {
        CompleteObjective,  // Hoàn thành 1 objective trong quest
        StartQuest,         // Bắt đầu quest mới
    }

    [Tooltip("Khi nào kích hoạt hành động này")]
    public When TriggerOn = When.OnDialogueEnd;

    [Tooltip("Loại hành động")]
    public ActionType Action = ActionType.CompleteObjective;

    [Tooltip("Quest liên quan (kéo SO asset vào đây)")]
    public QuestSO Quest;

    [Tooltip("ID objective cần hoàn thành — ví dụ: O1, O2  (chỉ dùng cho CompleteObjective)")]
    public string ObjectiveId;

    // ─── Static helper ───────────────────────────────────────────────────

    /// <summary>
    /// Thực thi tất cả action trong danh sách khớp với <paramref name="when"/>.
    /// Gọi từ bất kỳ component nào: QuestAction.Execute(actions, When.OnDialogueEnd)
    /// </summary>
    public static void Execute(List<QuestAction> actions, When when)
    {
        if (actions == null) return;

        foreach (var a in actions)
        {
            if (a.TriggerOn != when || a.Quest == null) continue;

            switch (a.Action)
            {
                case ActionType.CompleteObjective:
                    if (string.IsNullOrEmpty(a.ObjectiveId))
                    {
                        Debug.LogWarning($"[QUEST ACTION] CompleteObjective: ObjectiveId trống (Quest={a.Quest.Id})");
                        break;
                    }
                    Debug.Log($"[QUEST ACTION] CompleteObjective {a.Quest.Id}/{a.ObjectiveId}");
                    QuestManager.Instance?.CompleteObjective(a.Quest.Id, a.ObjectiveId);
                    break;

                case ActionType.StartQuest:
                    Debug.Log($"[QUEST ACTION] StartQuest {a.Quest.Id}");
                    QuestManager.Instance?.StartQuest(a.Quest);
                    break;
            }
        }
    }
}
