using System;

/// <summary>
/// Static event bus cho hệ thống quest.
/// Cho phép các hệ thống khác (UI, BattleManager…) lắng nghe
/// mà không cần tham chiếu trực tiếp đến QuestManager.
/// </summary>
public static class QuestEvents
{
    /// <summary>Được phát khi một quest mới bắt đầu.</summary>
    public static event Action<QuestSO> OnQuestStarted;

    /// <summary>Được phát khi một objective hoàn thành. Truyền questId và objectiveId.</summary>
    public static event Action<string, string> OnObjectiveCompleted;

    /// <summary>Được phát khi tất cả objectives của một quest đã done.</summary>
    public static event Action<QuestSO> OnQuestCompleted;

    // ─── Internal invokers (chỉ QuestManager gọi) ───────────────────────

    internal static void RaiseQuestStarted(QuestSO quest)
        => OnQuestStarted?.Invoke(quest);

    internal static void RaiseObjectiveCompleted(string questId, string objectiveId)
        => OnObjectiveCompleted?.Invoke(questId, objectiveId);

    internal static void RaiseQuestCompleted(QuestSO quest)
        => OnQuestCompleted?.Invoke(quest);
}
