using System;

/// <summary>
/// Một lựa chọn phân nhánh trong quest.
/// Khi người chơi chọn, quest có Id = LeadsToQuestId sẽ được kích hoạt.
/// </summary>
[Serializable]
public class QuestBranchChoice
{
    /// <summary>ID của lựa chọn, ví dụ "B1".</summary>
    public string Id;

    /// <summary>Mô tả hiển thị trên nút bấm.</summary>
    public string Description;

    /// <summary>Id của quest sẽ bắt đầu khi người chơi chọn nhánh này.</summary>
    public string LeadsToQuestId;
}
