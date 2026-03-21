using System;

/// <summary>
/// Một lựa chọn rẽ nhánh trong một quest.
/// Kéo QuestSO asset vào LeadsToQuest để chỉ định quest tiếp theo.
/// </summary>
[Serializable]
public class QuestBranchChoice
{
    /// <summary>ID duy nhất, ví dụ "B1".</summary>
    public string Id;

    /// <summary>Mô tả lựa chọn hiển thị cho người chơi.</summary>
    [UnityEngine.TextArea]
    public string Description;

    /// <summary>Quest sẽ mở khi chọn nhánh này (kéo SO asset vào đây).</summary>
    public QuestSO LeadsToQuest;
}
