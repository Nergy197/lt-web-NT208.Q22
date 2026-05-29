using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tự cấu hình BattleQuestTrigger cho Q002 (thua bắt buộc → O4 + về Chapter5_MapBattle).
/// Gắn vào Chapter5a_Battle (cùng object Chapter5a_BattleBootstrap hoặc QuestSystems).
/// </summary>
[DefaultExecutionOrder(-50)]
public class Chapter1BattleQuestSetup : MonoBehaviour
{
    [SerializeField] string questId = "Q002";
    [SerializeField] string lossObjectiveId = "O4";
    [SerializeField] string returnScene = "Chapter5_MapBattle";

    void Awake()
    {
        var trigger = GetComponent<BattleQuestTrigger>();
        if (trigger == null)
            trigger = gameObject.AddComponent<BattleQuestTrigger>();

        QuestSO quest = ResolveQuest(questId);
        if (quest == null)
        {
            Debug.LogWarning("[Chapter1BattleQuest] Không tìm thấy QuestSO — bỏ qua cấu hình battle quest.");
            return;
        }

        if (trigger.onLoseActions == null || trigger.onLoseActions.Count == 0)
        {
            trigger.onLoseActions = new List<QuestAction>
            {
                new QuestAction
                {
                    TriggerOn = QuestAction.When.OnBattleLoss,
                    Action = QuestAction.ActionType.CompleteObjective,
                    Quest = quest,
                    ObjectiveId = lossObjectiveId
                },
                new QuestAction
                {
                    TriggerOn = QuestAction.When.OnBattleLoss,
                    Action = QuestAction.ActionType.LoadScene,
                    SceneName = returnScene
                }
            };
            Debug.Log("[Chapter1BattleQuest] Đã cấu hình onLoseActions cho Q002.");
        }
    }

    static QuestSO ResolveQuest(string id)
    {
        if (QuestManager.Instance == null) return null;

        foreach (var q in QuestManager.Instance.AllQuests)
            if (q != null && q.Id == id) return q;

        return QuestManager.Instance.GetActiveQuest(id);
    }
}
