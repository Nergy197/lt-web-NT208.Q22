using System;

/// <summary>
/// Một mục tiêu (objective) trong một quest.
/// </summary>
[Serializable]
public class QuestObjective
{
    /// <summary>ID duy nhất, ví dụ "O1".</summary>
    public string Id;

    /// <summary>Mô tả hiển thị cho người chơi.</summary>
    public string Description;

    /// <summary>True khi objective đã hoàn thành.</summary>
    public bool IsCompleted;
}
