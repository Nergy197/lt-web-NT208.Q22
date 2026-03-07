using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// EditMode tests cho QuestManager và Chapter 1 quest line.
/// Chạy: Window → General → Test Runner → EditMode → Run All
/// </summary>
[TestFixture]
public class QuestManagerTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────

    static QuestSO MakeQuest(string id, string title, bool isMain = true,
        List<QuestObjective> objectives = null,
        List<QuestBranchChoice> branches = null)
    {
        var q = ScriptableObject.CreateInstance<QuestSO>();
        q.Id          = id;
        q.Title       = title;
        q.IsMainQuest  = isMain;
        q.Objectives   = objectives ?? new List<QuestObjective>
        {
            new QuestObjective { Id = "O1", Description = "Objective 1" },
            new QuestObjective { Id = "O2", Description = "Objective 2" },
        };
        q.BranchChoices = branches ?? new List<QuestBranchChoice>();
        return q;
    }

    /// <summary>Tạo QuestManager với list quests cho trước.</summary>
    static QuestManager MakeManager(params QuestSO[] quests)
    {
        var go  = new GameObject("QuestManager_Test");
        var mgr = go.AddComponent<QuestManager>();

        // Inject Instance qua reflection để tránh DontDestroyOnLoad trong tests
        typeof(QuestManager)
            .GetProperty("Instance",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, mgr);

        mgr.allQuests = new List<QuestSO>(quests);
        return mgr;
    }

    [TearDown]
    public void TearDown()
    {
        // Destroy tất cả QuestManager trong scene
        foreach (var go in Object.FindObjectsByType<QuestManager>(FindObjectsSortMode.None))
            Object.DestroyImmediate(go.gameObject);

        // Dọn PlayerPrefs để test save/load không ảnh hưởng lẫn nhau
        PlayerPrefs.DeleteKey(QuestManager.SaveKey);
        PlayerPrefs.Save();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    //  Core API Tests
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Test]
    public void StartQuest_AddsToActiveList()
    {
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");

        Assert.AreEqual(1, mgr.ActiveQuests.Count);
        Assert.AreEqual("Q001", mgr.ActiveQuests[0].Id);
    }

    [Test]
    public void StartQuest_Duplicate_IsIgnored()
    {
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");
        mgr.StartQuest("Q001"); // lần 2 phải bị bỏ qua

        Assert.AreEqual(1, mgr.ActiveQuests.Count);
    }

    [Test]
    public void CompleteObjective_MarksObjective()
    {
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");
        mgr.CompleteObjective("Q001", "O1");

        Assert.IsTrue(quest.Objectives[0].IsCompleted);
        Assert.IsFalse(quest.Objectives[1].IsCompleted);
    }

    [Test]
    public void AllObjectivesDone_QuestIsCompleted()
    {
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");
        mgr.CompleteObjective("Q001", "O1");
        mgr.CompleteObjective("Q001", "O2");

        Assert.IsTrue(quest.IsCompleted);
    }

    [Test]
    public void CompletedQuest_MovedToCompletedList()
    {
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");
        mgr.CompleteObjective("Q001", "O1");
        mgr.CompleteObjective("Q001", "O2");

        Assert.AreEqual(0, mgr.ActiveQuests.Count);
        Assert.AreEqual(1, mgr.CompletedQuests.Count);
    }

    [Test]
    public void FiresEvent_OnObjectiveCompleted()
    {
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        string capturedQid = null, capturedOid = null;
        void Handler(string qid, string oid) { capturedQid = qid; capturedOid = oid; }
        QuestEvents.OnObjectiveCompleted += Handler;

        try
        {
            mgr.StartQuest("Q001");
            mgr.CompleteObjective("Q001", "O1");

            Assert.AreEqual("Q001", capturedQid);
            Assert.AreEqual("O1",   capturedOid);
        }
        finally
        {
            QuestEvents.OnObjectiveCompleted -= Handler;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    //  Chapter 1 Quest Line Tests
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// <summary>
    /// Xây dựng manager với đủ 6 quest Chapter 1 và
    /// gắn auto-progression handler (giả lập HandleQuestCompleted).
    /// </summary>
    static QuestManager MakeChapter1Manager()
    {
        // Build quests giống Chapter1QuestData.BuildQuests()
        var quests = new List<QuestSO>
        {
            MakeQuest("Q001", "Di nguyện của cha",    isMain: true,
                objectives: new List<QuestObjective> {
                    new QuestObjective { Id = "O1", Description = "Đến bên cha lần cuối" },
                    new QuestObjective { Id = "O2", Description = "Nhận lời thề thiêng liêng" },
                }),
            MakeQuest("Q002", "Bước vào chiến tranh", isMain: true,
                objectives: new List<QuestObjective> {
                    new QuestObjective { Id = "O1", Description = "Nhận lệnh điều binh" },
                    new QuestObjective { Id = "O2", Description = "Dẫn quân ra trận" },
                    new QuestObjective { Id = "O3", Description = "Đánh giá thực lực địch" },
                }),
            MakeQuest("Q003", "Thất bại đầu tiên",    isMain: true,
                objectives: new List<QuestObjective> {
                    new QuestObjective { Id = "O1", Description = "Đối diện thất bại" },
                    new QuestObjective { Id = "O2", Description = "Ra lệnh rút lui" },
                }),
            MakeQuest("Q004", "Đất nước trong tay",   isMain: true,
                objectives: new List<QuestObjective> {
                    new QuestObjective { Id = "O1", Description = "Nhận tin quân rút" },
                    new QuestObjective { Id = "O2", Description = "Trở về kinh đô" },
                }),
            MakeQuest("Q005", "Ngã ba số phận",        isMain: true,
                objectives: new List<QuestObjective> {
                    new QuestObjective { Id = "O1", Description = "Suy ngẫm về lựa chọn" },
                },
                branches: new List<QuestBranchChoice> {
                    new QuestBranchChoice { Id = "B1", Description = "Theo đuổi quyền lực", LeadsToQuestId = "Q006" },
                    new QuestBranchChoice { Id = "B2", Description = "Bảo vệ đất nước",     LeadsToQuestId = "Q007" },
                }),
            MakeQuest("Q010", "Hậu quả chiến tranh",  isMain: false,
                objectives: new List<QuestObjective> {
                    new QuestObjective { Id = "O1", Description = "Đi qua làng bị tàn phá" },
                    new QuestObjective { Id = "O2", Description = "Nói chuyện dân làng" },
                    new QuestObjective { Id = "O3", Description = "Ghi nhận nỗi đau" },
                }),
            // Placeholder quests cho Chapter 2 (chỉ cần tồn tại để ChooseBranch không warn)
            MakeQuest("Q006", "Chapter 2A – Quyền lực",    isMain: true,
                objectives: new List<QuestObjective> { new QuestObjective { Id = "O1", Description = "..." } }),
            MakeQuest("Q007", "Chapter 2B – Đất nước",     isMain: true,
                objectives: new List<QuestObjective> { new QuestObjective { Id = "O1", Description = "..." } }),
        };

        var mgr = MakeManager(quests.ToArray());

        // Đăng ký auto-progression (giống QuestManager.Start() thật)
        QuestEvents.OnQuestCompleted += mgr_HandleQuestCompleted;
        void mgr_HandleQuestCompleted(QuestSO q)
        {
            switch (q.Id)
            {
                case "Q001": mgr.StartQuest("Q002"); break;
                case "Q002": mgr.StartQuest("Q003"); break;
                case "Q003": mgr.StartQuest("Q004"); mgr.StartQuest("Q010"); break;
                case "Q004": mgr.StartQuest("Q005"); break;
            }
        }

        return mgr;
    }

    [Test]
    public void Ch1_Q001Complete_AutoStartsQ002()
    {
        var mgr = MakeChapter1Manager();
        mgr.StartQuest("Q001");

        mgr.CompleteObjective("Q001", "O1");
        mgr.CompleteObjective("Q001", "O2");

        Assert.IsNotNull(mgr.GetActiveQuest("Q002"),
            "Q002 phải tự động bắt đầu sau khi Q001 hoàn thành.");
    }

    [Test]
    public void Ch1_Q003Complete_Opens_Q004_And_Q010()
    {
        var mgr = MakeChapter1Manager();
        mgr.StartQuest("Q001");

        // Hoàn thành Q001
        mgr.CompleteObjective("Q001", "O1");
        mgr.CompleteObjective("Q001", "O2");

        // Hoàn thành Q002
        mgr.CompleteObjective("Q002", "O1");
        mgr.CompleteObjective("Q002", "O2");
        mgr.CompleteObjective("Q002", "O3");

        // Hoàn thành Q003
        mgr.CompleteObjective("Q003", "O1");
        mgr.CompleteObjective("Q003", "O2");

        Assert.IsNotNull(mgr.GetActiveQuest("Q004"),
            "Q004 phải active sau Q003.");
        Assert.IsNotNull(mgr.GetActiveQuest("Q010"),
            "Q010 (side quest) phải mở song song với Q004.");
    }

    [Test]
    public void Ch1_Q005_ChooseBranch_B1_StartsQ006()
    {
        var mgr = MakeChapter1Manager();

        // Simulate đến Q005 bằng cách StartQuest trực tiếp
        mgr.StartQuest("Q005");
        mgr.ChooseBranch("Q005", "B1");

        Assert.IsNotNull(mgr.GetActiveQuest("Q006"),
            "Chọn B1 → Q006 (Chapter 2A) phải được StartQuest.");
    }

    [Test]
    public void Ch1_Q005_ChooseBranch_B2_StartsQ007()
    {
        var mgr = MakeChapter1Manager();

        mgr.StartQuest("Q005");
        mgr.ChooseBranch("Q005", "B2");

        Assert.IsNotNull(mgr.GetActiveQuest("Q007"),
            "Chọn B2 → Q007 (Chapter 2B) phải được StartQuest.");
    }

    [Test]
    public void Ch1_Q010_SideQuest_CanCompleteIndependently()
    {
        var mgr = MakeChapter1Manager();
        mgr.StartQuest("Q010");

        mgr.CompleteObjective("Q010", "O1");
        mgr.CompleteObjective("Q010", "O2");
        mgr.CompleteObjective("Q010", "O3");

        Assert.IsTrue(mgr.IsQuestCompleted("Q010"),
            "Q010 hoàn thành khi đủ 3 objectives.");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    //  Save / Load Tests
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━


    [Test]
    public void SaveAndLoad_RestoresActiveQuestWithObjectives()
    {
        // ── Arrange ──────────────────────────────────────────────────────
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");
        mgr.CompleteObjective("Q001", "O1"); // O1 done, O2 còn lại

        // ── Act: save, rồi tạo manager mới và load ────────────────────
        mgr.SaveProgress();

        var quest2 = MakeQuest("Q001", "Di nguyện của cha"); // SO mới (sạch)
        var mgr2   = MakeManager(quest2);
        mgr2.LoadProgress();

        // ── Assert ───────────────────────────────────────────────────────
        Assert.AreEqual(1, mgr2.ActiveQuests.Count,    "Q001 phải còn active sau khi load.");
        Assert.IsTrue(quest2.Objectives[0].IsCompleted, "O1 phải được restore về complete.");
        Assert.IsFalse(quest2.Objectives[1].IsCompleted,"O2 phải vẫn là chưa complete.");
    }

    [Test]
    public void SaveAndLoad_RestoresCompletedQuest()
    {
        // ── Arrange ──────────────────────────────────────────────────────
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);

        mgr.StartQuest("Q001");
        mgr.CompleteObjective("Q001", "O1");
        mgr.CompleteObjective("Q001", "O2"); // quest hoàn thành

        // ── Act ───────────────────────────────────────────────────────────
        mgr.SaveProgress();

        var quest2 = MakeQuest("Q001", "Di nguyện của cha");
        var mgr2   = MakeManager(quest2);
        mgr2.LoadProgress();

        // ── Assert ───────────────────────────────────────────────────────
        Assert.AreEqual(0, mgr2.ActiveQuests.Count,     "Active phải rỗng sau load.");
        Assert.AreEqual(1, mgr2.CompletedQuests.Count,  "Q001 phải nằm trong CompletedQuests.");
        Assert.IsTrue(mgr2.IsQuestCompleted("Q001"),    "IsQuestCompleted phải trả về true.");
    }

    [Test]
    public void ClearSave_RemovesPlayerPrefsKey()
    {
        // ── Arrange ──────────────────────────────────────────────────────
        var quest = MakeQuest("Q001", "Di nguyện của cha");
        var mgr   = MakeManager(quest);
        mgr.StartQuest("Q001");
        mgr.SaveProgress();

        Assert.IsTrue(PlayerPrefs.HasKey(QuestManager.SaveKey), "Key phải tồn tại sau Save.");

        // ── Act ───────────────────────────────────────────────────────────
        mgr.ClearSave();

        // ── Assert ───────────────────────────────────────────────────────
        Assert.IsFalse(PlayerPrefs.HasKey(QuestManager.SaveKey), "Key phải bị xóa sau ClearSave.");
    }
}
