using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global pub/sub event bus. Dùng để giao tiếp giữa các module mà không cần
/// tham chiếu trực tiếp lẫn nhau.
///
/// Cách dùng:
///   Subscribe:   EventManager.Subscribe(GameEvent.BattleStart, OnBattleStart);
///   Unsubscribe: EventManager.Unsubscribe(GameEvent.BattleStart, OnBattleStart);
///   Publish:     EventManager.Publish(GameEvent.BattleStart);
///   Publish:     EventManager.Publish(GameEvent.UnitDied, someStatus);
/// </summary>
public static class EventManager
{
    private static readonly Dictionary<GameEvent, List<Action<object>>> listeners
        = new Dictionary<GameEvent, List<Action<object>>>();

    // ================= SUBSCRIBE =================

    public static void Subscribe(GameEvent eventType, Action<object> callback)
    {
        if (!listeners.ContainsKey(eventType))
            listeners[eventType] = new List<Action<object>>();

        if (!listeners[eventType].Contains(callback))
            listeners[eventType].Add(callback);
    }

    // ================= UNSUBSCRIBE =================

    public static void Unsubscribe(GameEvent eventType, Action<object> callback)
    {
        if (listeners.TryGetValue(eventType, out var list))
            list.Remove(callback);
    }

    // ================= PUBLISH =================

    public static void Publish(GameEvent eventType, object payload = null)
    {
        if (!listeners.TryGetValue(eventType, out var list) || list.Count == 0)
            return;

        // Copy để tránh lỗi nếu callback tự Unsubscribe trong khi đang duyệt
        var snapshot = new List<Action<object>>(list);

        foreach (var callback in snapshot)
        {
            try
            {
                callback?.Invoke(payload);
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventManager] Exception in subscriber of {eventType}: {e}");
            }
        }

        Debug.Log($"[EVENT] {eventType}" + (payload != null ? $" | payload: {payload.GetType().Name}" : ""));
    }

    // ================= CLEAR (dùng khi test hoặc reset game) =================

    public static void Clear()
    {
        listeners.Clear();
    }
}
