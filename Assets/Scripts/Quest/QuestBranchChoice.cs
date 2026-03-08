using System;

/// <summary>
/// Một lựa chọn rẽ nhánh (branch choice) trong một quest.
/// Dùng cho các quest có nhiều hướng đi khác nhau.
/// </summary>
[Serializable]
public class QuestBranchChoice
{
    /// <summary>ID duy nhất, ví dụ "B1".</summary>
    public string Id;

    /// <summary>Mô tả lựa chọn hiển thị cho người chơi.</summary>
    public string Description;

    /// <summary>ID của quest tiếp theo khi chọn nhánh này.</summary>
    public string LeadsToQuestId;
}
