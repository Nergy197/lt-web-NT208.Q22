using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject trong BattleScene để kích hoạt quest actions khi trận đấu kết thúc.
/// Thay thế cho việc hardcode quest objectives trong BattleManager.
///
/// Setup:
///   1. Gắn script này vào một GameObject trong BattleScene
///   2. Kéo QuestSO và cấu hình actions trong Inspector
///   3. Chọn TriggerOn = OnBattleWin hoặc OnBattleLoss
/// </summary>
public class BattleQuestTrigger : MonoBehaviour
{
    [Header("Quest Actions — Khi thắng trận")]
    [Tooltip("Các quest action tự động chạy khi player thắng trận.")]
    public List<QuestAction> onWinActions = new();

    [Header("Quest Actions — Khi thua trận")]
    [Tooltip("Các quest action tự động chạy khi player thua trận.")]
    public List<QuestAction> onLoseActions = new();

    [Header("Quest Actions — Bất kể thắng/thua")]
    [Tooltip("Các quest action luôn chạy khi trận kết thúc (kể cả bỏ chạy).")]
    public List<QuestAction> onAnyEndActions = new();

    void OnEnable()
    {
        EventManager.Subscribe(GameEvent.BattleWin, OnBattleWin);
        EventManager.Subscribe(GameEvent.BattleLose, OnBattleLose);
        EventManager.Subscribe(GameEvent.BattleFlee, OnBattleFlee);
    }

    void OnDisable()
    {
        EventManager.Unsubscribe(GameEvent.BattleWin, OnBattleWin);
        EventManager.Unsubscribe(GameEvent.BattleLose, OnBattleLose);
        EventManager.Unsubscribe(GameEvent.BattleFlee, OnBattleFlee);
    }

    void OnBattleWin(object _)
    {
        QuestAction.Execute(onWinActions, QuestAction.When.OnBattleWin);
        QuestAction.Execute(onAnyEndActions, QuestAction.When.Manual);
    }

    void OnBattleLose(object _)
    {
        QuestAction.Execute(onLoseActions, QuestAction.When.OnBattleLoss);
        QuestAction.Execute(onAnyEndActions, QuestAction.When.Manual);
    }

    void OnBattleFlee(object _)
    {
        QuestAction.Execute(onAnyEndActions, QuestAction.When.Manual);
    }
}
