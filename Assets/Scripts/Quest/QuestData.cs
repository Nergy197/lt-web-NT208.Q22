using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa toàn bộ dữ liệu quest của Chapter 1.
/// Tạo asset: chuột phải → Create → Quests → Chapter 1 Data
/// Sau đó kéo vào QuestManager.allQuests.
/// </summary>
[CreateAssetMenu(menuName = "Quests/Chapter 1 Data", fileName = "Chapter1QuestData")]
public class Chapter1QuestData : ScriptableObject
{
    // ─── Tự tạo danh sách quests khi được gọi ────────────────────────────

    /// <summary>
    /// Trả về tất cả quest của Chapter 1 (5 main + 1 side).
    /// Dùng để đăng ký vào QuestManager.allQuests.
    /// </summary>
    public List<QuestSO> BuildQuests()
    {
        return new List<QuestSO>
        {
            BuildQ001(),
            BuildQ002(),
            BuildQ003(),
            BuildQ004(),
            BuildQ005(),
            BuildQ010(),
        };
    }

    // ─── Q001 ─────────────────────────────────────────────────────────────

    static QuestSO BuildQ001()
    {
        var q = CreateInstance<QuestSO>();
        q.Id          = "Q001";
        q.Title       = "Di nguyện của cha";
        q.Description = "Cha ta — Trần Liễu — đã ra đi, mang theo nỗi hận không thể nói thành lời. Ta đang mang nó theo.";
        q.IsMainQuest  = true;
        q.Objectives   = new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Đến bên cha lần cuối" },
            new QuestObjective { Id = "O2", Description = "Nhận lời thề thiêng liêng" },
        };
        q.BranchChoices = new List<QuestBranchChoice>();
        return q;
    }

    // ─── Q002 ─────────────────────────────────────────────────────────────

    static QuestSO BuildQ002()
    {
        var q = CreateInstance<QuestSO>();
        q.Id          = "Q002";
        q.Title       = "Bước vào chiến tranh";
        q.Description = "Kẻ thù đến từ phương Bắc. Giỏi hơn, mạnh hơn, và tàn nhẫn hơn bất cứ điều gì ta từng đối mặt.";
        q.IsMainQuest  = true;
        q.Objectives   = new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Nhận lệnh điều binh từ triều đình" },
            new QuestObjective { Id = "O2", Description = "Dẫn quân ra trận lần đầu" },
            new QuestObjective { Id = "O3", Description = "Đánh giá thực lực của địch" },
        };
        q.BranchChoices = new List<QuestBranchChoice>();
        return q;
    }

    // ─── Q003 ─────────────────────────────────────────────────────────────

    static QuestSO BuildQ003()
    {
        var q = CreateInstance<QuestSO>();
        q.Id          = "Q003";
        q.Title       = "Thất bại đầu tiên";
        q.Description = "Không phải ta không đủ dũng cảm. Nhưng lòng dũng cảm không thắng được bằng chiến thuật mà ta chưa biết.";
        q.IsMainQuest  = true;
        q.Objectives   = new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Đối diện với thất bại trong trận chiến" },
            new QuestObjective { Id = "O2", Description = "Ra lệnh rút lui và đưa quân trở về an toàn" },
        };
        q.BranchChoices = new List<QuestBranchChoice>();
        return q;
    }

    // ─── Q004 ─────────────────────────────────────────────────────────────

    static QuestSO BuildQ004()
    {
        var q = CreateInstance<QuestSO>();
        q.Id          = "Q004";
        q.Title       = "Đất nước trong tay";
        q.Description = "Quân Mông Cổ rút lui sau thất bại ở Đông Bộ Đầu. Đất nước sống sót — nhưng cái giá phải trả là rất lớn.";
        q.IsMainQuest  = true;
        q.Objectives   = new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Nhận tin quân Mông Cổ rút lui" },
            new QuestObjective { Id = "O2", Description = "Trở về kinh đô" },
        };
        q.BranchChoices = new List<QuestBranchChoice>();
        return q;
    }

    // ─── Q005 (Branching) ─────────────────────────────────────────────────

    static QuestSO BuildQ005()
    {
        var q = CreateInstance<QuestSO>();
        q.Id          = "Q005";
        q.Title       = "Ngã ba số phận";
        q.Description = "Ta có hai con đường. Một là lời thề với cha. Một là lương tâm của ta. Ta không thể chọn cả hai.";
        q.IsMainQuest  = true;
        q.Objectives   = new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Suy ngẫm về lựa chọn trước mặt" },
        };
        q.BranchChoices = new List<QuestBranchChoice>
        {
            new QuestBranchChoice
            {
                Id              = "B1",
                Description     = "Di nguyện của cha phải được thực hiện. Ta sẽ đoạt lại ngôi vị.",
                LeadsToQuestId  = "Q006"
            },
            new QuestBranchChoice
            {
                Id              = "B2",
                Description     = "Đất nước cần ta hơn. Nỗi hận của cha không thể là lý do để ta phản bội người dân.",
                LeadsToQuestId  = "Q007"
            },
        };
        return q;
    }

    // ─── Q010 (Side Quest) ────────────────────────────────────────────────

    static QuestSO BuildQ010()
    {
        var q = CreateInstance<QuestSO>();
        q.Id          = "Q010";
        q.Title       = "Hậu quả chiến tranh";
        q.Description = "Làng bị bỏ hoang. Nhà bị đốt. Người dân mất tất cả. Mỗi quyết định của ta đều có cái giá của nó.";
        q.IsMainQuest  = false;
        q.Objectives   = new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Đi qua ngôi làng bị tàn phá" },
            new QuestObjective { Id = "O2", Description = "Nói chuyện với người dân còn sót lại" },
            new QuestObjective { Id = "O3", Description = "Ghi nhận nỗi đau của họ" },
        };
        q.BranchChoices = new List<QuestBranchChoice>();
        return q;
    }
}
