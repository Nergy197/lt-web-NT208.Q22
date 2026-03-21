using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đặt trên một GameObject có Collider2D (IsTrigger = true).
/// Kích hoạt Quest Actions khi player bước vào / ra khỏi vùng.
///
/// Setup:
///   1. Tạo empty GameObject, gắn BoxCollider2D với IsTrigger = true
///   2. Gắn script này
///   3. Thêm Quest Actions: kéo QuestSO, chọn TriggerOn = OnEnterZone hoặc OnExitZone
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuestZoneTrigger : MonoBehaviour
{
    [Tooltip("Chỉ kích hoạt 1 lần. Bỏ tick nếu muốn lặp lại mỗi lần vào.")]
    public bool triggerOnce = true;

    [Header("Quest Actions")]
    [Tooltip("Kéo QuestSO vào Quest, chọn TriggerOn = OnEnterZone hoặc OnExitZone.")]
    public List<QuestAction> questActions = new();

    bool _triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && _triggered) return;

        _triggered = true;
        QuestAction.Execute(questActions, QuestAction.When.OnEnterZone);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        QuestAction.Execute(questActions, QuestAction.When.OnExitZone);
    }

    /// <summary>Reset để trigger lại (dùng khi load save hoặc test).</summary>
    public void ResetTrigger() => _triggered = false;
}
