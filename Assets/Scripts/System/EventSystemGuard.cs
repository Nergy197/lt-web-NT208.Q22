using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// Đảm bảo mọi scene luôn có EventSystem.
/// Chapter4_WarNews và Chapter6_Village không có EventSystem trong scene,
/// nên script này tự tạo một cái nếu thiếu.
public class EventSystemGuard : MonoBehaviour
{
    static EventSystemGuard _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (_instance != null) return;
        var go = new GameObject("[EventSystemGuard]");
        _instance = go.AddComponent<EventSystemGuard>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var go = new GameObject("[AutoEventSystem]");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            Debug.Log($"[EventSystemGuard] Tạo EventSystem tự động cho scene: {scene.name}");
        }
    }
}
