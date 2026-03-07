using System;
using System.Collections.Generic;

/// <summary>
/// Dữ liệu của một objective cần lưu.
/// </summary>
[Serializable]
public class ObjectiveSaveData
{
    public string Id;
    public bool   IsCompleted;
}

/// <summary>
/// Dữ liệu tiến độ của một quest cần lưu.
/// </summary>
[Serializable]
public class QuestProgressData
{
    /// <summary>Id của quest (khớp với QuestSO.Id).</summary>
    public string QuestId;

    /// <summary>"Active" hoặc "Completed".</summary>
    public string Status;

    /// <summary>Trạng thái từng objective tại thời điểm lưu.</summary>
    public List<ObjectiveSaveData> Objectives = new();
}

/// <summary>
/// Container gốc chứa toàn bộ tiến độ quest.
/// Được serialize ra JSON và lưu vào PlayerPrefs["QuestSave"].
/// </summary>
[Serializable]
public class AllQuestsSaveData
{
    public List<QuestProgressData> Quests = new();
}
