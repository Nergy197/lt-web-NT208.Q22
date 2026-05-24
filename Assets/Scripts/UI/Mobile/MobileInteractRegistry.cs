using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry nhỏ cho mobile UI biết khi nào thật sự cần hiện nút Interact.
/// Mỗi trigger/dialogue tự đăng ký khi player có thể tương tác và hủy khi không còn.
/// </summary>
public static class MobileInteractRegistry
{
    private static readonly HashSet<object> sources = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        sources.Clear();
    }

    public static void SetActive(object source, bool active)
    {
        if (source == null) return;

        if (active) sources.Add(source);
        else sources.Remove(source);
    }

    public static bool HasActiveInteraction
    {
        get
        {
            sources.RemoveWhere(source => source == null || (source is Object unityObject && unityObject == null));
            return sources.Count > 0;
        }
    }
}
